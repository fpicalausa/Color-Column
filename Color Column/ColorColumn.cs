using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows;
using Microsoft.VisualStudio.Text.Formatting;

namespace ColorColumn
{
    /// <summary>
    /// Adornment class that draws a square box in the top right hand corner of the viewport
    /// </summary>
    class ColorColumn
    {
        private IWpfTextView        _view;
        private IEditorFormatMap    _formatMap;
        private OptionsPage         _settings;

        private Rect?               _displayedRect;
        private int?                _displayedCursorColumn;
        private List<int>           _linesOffset;
        private List<int>           _columns;
        private List<int>           _cursorColumnList;

        private Brush               _columnBrush;
        private Brush               _cursorColumnBrush;
        private Pen                 _emptyPen;

        private IAdornmentLayer     _adornmentLayer;
        private Image               _columnsImage;
        private Image               _cursorColumnImage;

        private Brush getBrush(string name, Brush defaultValue) {
            const string key = EditorFormatDefinition.BackgroundBrushId;
            var properties = _formatMap.GetProperties(name);
            if (properties.Contains(key))
            {
                return (Brush)properties[key];
            }

            return defaultValue;
        }

        private void regenerateColumns()
        {
            _columns.Clear();
            _columns.AddRange(from column in _settings.ParsedColumns
                              orderby column
                              select column - 1);
        }

        private void updateSettings()
        {
            regenerateColumns();
            updateAdornments(true);
        }

        private void updateBrushes()
        {
            _columnBrush = getBrush(ColorColumnTextFormat.name,
                new SolidColorBrush(ColorColumnTextFormat.defaultBackgroundColor));
            _columnBrush.Freeze();

            _cursorColumnBrush = getBrush(CursorColumnFormat.name,
                new SolidColorBrush(CursorColumnFormat.defaultBackgroundColor));
            _cursorColumnBrush.Freeze();

            _emptyPen = new Pen();
        }

        private void computeLines()
        {
            int left = 0;
            _linesOffset.Clear();
            foreach (var line in _view.TextViewLines)
            {
                if (line.IsFirstTextViewLineForSnapshotLine) left = 0;
                _linesOffset.Add(left);
                left += line.Length;
            }
        }

        private double getColumnLeft(int viewColumn)
        {
            return _view.FormattedLineSource.BaseIndentation + 
                _view.FormattedLineSource.ColumnWidth * viewColumn - 
                _view.ViewportLeft - 1;
        }

        private int getFirstColumn()
        {
            double colwidth = _view.FormattedLineSource.ColumnWidth;
            int colleft = Convert.ToInt32((_view.ViewportLeft - _view.FormattedLineSource.BaseIndentation) / colwidth);
            return colleft < 0 ? 0 : colleft;
        }

        private int getLastColumn()
        {
            double colwidth = _view.FormattedLineSource.ColumnWidth;
            return Convert.ToInt32(((_view.ViewportRight - _view.FormattedLineSource.BaseIndentation) / colwidth + 1));
        }

        private void renderColumns(
            GeometryGroup geometry,
            List<int> columns,
            double top, double bottom, 
            int lineFirstColumn, int firstVisibleColumn, int lastVisibleColumn)
        {
            int firstColumn = firstVisibleColumn + lineFirstColumn;
            int lastColumn = lastVisibleColumn + lineFirstColumn;

            foreach (var column in columns)
            {
                if (column < firstColumn) continue;
                if (column >= lastColumn) break;

                double columnLeft = getColumnLeft(column - lineFirstColumn);

                System.Windows.Rect r = new System.Windows.Rect(
                    columnLeft,
                    top,
                    _view.FormattedLineSource.ColumnWidth,
                    bottom - top);
                geometry.Children.Add(new RectangleGeometry(r));
            }
        }

        private void renderColumnsForLines(GeometryGroup geometry, List<int> columns, ITextViewLineCollection lines)
        {
            double top = 0.0, bottom = _view.ViewportHeight;
            int lineindex = 0;
            bool resetTopAtNextLine = true;

            int leftmostcolumn = getFirstColumn();
            int rightmostcolumn = getLastColumn();

            foreach (var line in lines)
            {
                // Create new columns starting from this line
                if (resetTopAtNextLine)
                {
                    top = line.Top - _view.ViewportTop;
                }

                bottom = line.Bottom - _view.ViewportTop;

                // If next line is not starting at a different column than this 
                // one we need to draw this line immediately, and disregard
                // the current line's top for when drawing the next lines.
                resetTopAtNextLine = (!line.IsLastTextViewLineForSnapshotLine 
                    || !line.IsFirstTextViewLineForSnapshotLine);

                // We need to draw the current columns before next line
                if (resetTopAtNextLine)
                {
                    renderColumns(geometry, columns, top, bottom, 
                        _linesOffset[lineindex],
                        leftmostcolumn, 
                        rightmostcolumn);
                }

                lineindex++;
            }

            if (!resetTopAtNextLine && ((lineindex - 1) < _linesOffset.Count))
            {
                renderColumns(geometry, columns, top, bottom, 
                    _linesOffset[lineindex - 1],
                    leftmostcolumn, 
                    rightmostcolumn);
            }
        }

        private void renderColumns(List<int> columns, Brush brush, Image image)
        {
            GeometryGroup group = new GeometryGroup();
            group.Children.Add(new RectangleGeometry(new Rect(0, 0, 0, 0)));
            renderColumnsForLines(group, columns, _view.TextViewLines);
            group.Freeze();

            Drawing drawing = new GeometryDrawing(brush, _emptyPen, group);
            drawing.Freeze();

            DrawingImage drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();

            image.Source = drawingImage;

            Canvas.SetLeft(image, _view.ViewportLeft + group.Bounds.Left);
            Canvas.SetTop(image, _view.ViewportTop + group.Bounds.Top);
        }

        private int getCursorColumn()
        {
            var position = _view.Caret.Position;

            return position.BufferPosition.GetContainingLine().Start.Difference(
                    position.VirtualBufferPosition.Position) + position.VirtualSpaces;
        }

        private void paintColumns(bool force = true)
        {
            if (_view.FormattedLineSource == null) return; 
            bool forceUpdateCursorColumn = force;

            var displayRect = new Rect(_view.ViewportLeft, _view.ViewportTop, _view.ViewportWidth, _view.ViewportHeight);
            if (force || !_displayedRect.HasValue || _displayedRect.Value != displayRect) 
            {
                computeLines();
                renderColumns(_columns, _columnBrush, _columnsImage);
                _displayedRect = displayRect;

                forceUpdateCursorColumn = true;
            }

            int cursorColumn = getCursorColumn();
            if (_settings.CursorColumn && (forceUpdateCursorColumn || !_displayedCursorColumn.HasValue || cursorColumn != _displayedCursorColumn))
            {
                _cursorColumnList[0] = cursorColumn;
                renderColumns(_cursorColumnList, _cursorColumnBrush, _cursorColumnImage);
                _displayedCursorColumn = cursorColumn;
            }
        }

        private void updateAdornments(bool forced = false)
        {
            paintColumns(forced);
            _adornmentLayer.RemoveAllAdornments();
            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative,
                null, null, _cursorColumnImage, null);
            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative,
                null, null, _columnsImage, null);
        }

        private void resetDrawing()
        {
            updateBrushes();
            paintColumns();
        }

        /// <summary>
        /// Creates a square image and attaches an event handler to the layout changed event that
        /// adds the the square in the upper right-hand corner of the TextView via the adornment layer
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        public ColorColumn(IWpfTextView textView, IEditorFormatMap formatMap, OptionsPage settings)
        {
            _view = textView;
            _formatMap = formatMap;
            _settings = settings;
            _columns = new List<int>();
            _cursorColumnList = new List<int>();
            _linesOffset = new List<int>();

            _cursorColumnList.Add(0);               //Dummy initial position;

            _adornmentLayer = _view.GetAdornmentLayer("ColorColumn");
            _columnsImage = new Image();
            _cursorColumnImage = new Image();

            _settings.PropertyChanged += settings_PropertyChanged;
            _view.LayoutChanged += view_LayoutChanged;
            _view.Caret.PositionChanged += caret_PositionChanged;
            _formatMap.FormatMappingChanged += _formatMap_FormatMappingChanged;

            resetDrawing();
            updateSettings();
        }

        private void _formatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            updateBrushes();
            updateAdornments(true);
        }

        private void settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateSettings();
        }

        private void caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!_settings.CursorColumn) return;
            updateAdornments();
        }

        private void view_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            updateAdornments(e.HorizontalTranslation || e.VerticalTranslation);
        }
    }
}