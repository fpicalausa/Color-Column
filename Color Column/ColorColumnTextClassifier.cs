using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace ColorColumn
{
    #region Classification type definition
    internal static class ColorColumnTextClassificationDefinition
    {
        /// <summary>
        /// Defines the "EditorClassifier1" classification type.
        /// </summary>
        // MEF
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("Color Column Text")]
        internal static ClassificationTypeDefinition ColorColumnTextType = null;
    }
    #endregion

    #region Provider definition
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ClassificationTag))]
    internal class ColorColumnTaggerProvider : IViewTaggerProvider
    {
        /// <summary>
        /// Import the classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

        [Import]
        internal SVsServiceProvider serviceProvider = null; // Set via MEF

        private ColorColumnPackage getPackage()
        {
            IVsPackage package; 
            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            Marshal.ThrowExceptionForHR(shell.LoadPackage(new Guid(GuidList.colorColumnPkgString), out package));

            return package as ColorColumnPackage;
        }

        private OptionsPage getSettings()
        {
            return getPackage().Settings;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null || textView.TextBuffer != buffer) return null;
            
            return new ColorColumnTextClassifier(ClassificationRegistry, getSettings(), buffer, textView) as ITagger<T>;
        }
    }
    #endregion //provider def

    #region Classifier
    /// <summary>
    /// Classifier that classifies all text as an instance of the OrinaryClassifierType
    /// </summary>
    class ColorColumnTextClassifier : ITagger<ClassificationTag>
    {
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        OptionsPage         _options;
        List<int>           _columns;
        ITextBuffer         _buffer;
        ITextView           _view;
        ClassificationTag   _tag;
        object updateLock = new object();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        internal ColorColumnTextClassifier(IClassificationTypeRegistryService registry, OptionsPage options, ITextBuffer buffer, ITextView view)
        {
            _tag = new ClassificationTag(registry.GetClassificationType("Color Column Text"));
            _options = options;
            _columns = new List<int>();
            _buffer = buffer;
            _view = view;

            _view.LayoutChanged += _view_LayoutChanged;

            _options.PropertyChanged += _options_PropertyChanged;
            updateColumns();
        }

        void SynchronousUpdate(IEnumerable<SnapshotSpan> lines)
        {
            lock (updateLock)
            {
                var tempEvent = TagsChanged;
                if (tempEvent != null) {
                    foreach (var item in lines) {
                        tempEvent(this, new SnapshotSpanEventArgs(item));
                    }
                }
            }
        }

        private void _view_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.OldSnapshot != e.NewSnapshot)
            {
                SynchronousUpdate(from line in e.NewOrReformattedLines select line.Extent);
            }

        }

        private void updateColumns()
        {
            _columns.Clear();
            _columns.AddRange(from int column in _options.ParsedColumns
                              orderby column
                              select column - 1);
        }

        private void _options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateColumns();
            var tmp = ClassificationChanged;
            if (tmp != null)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                tmp(this, new ClassificationChangedEventArgs(
                    new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
        }

        private static bool isNonSpacingCharacter(char c)
        {
            // See https://github.com/jaredpar/VsVim/blob/master/VimCore/ColumnWiseUtil.fs
            switch (System.Char.GetUnicodeCategory(c))
            {
                case System.Globalization.UnicodeCategory.Control:
                case System.Globalization.UnicodeCategory.NonSpacingMark:
                case System.Globalization.UnicodeCategory.Format:
                case System.Globalization.UnicodeCategory.EnclosingMark:
                    return true;
                default:
                    return (c == '\u200b') || ('\u1160' <= c && c <= '\u11ff');
            }
        }

        private static bool isWideCharacter(char c)
        {
            // See https://github.com/jaredpar/VsVim/blob/master/VimCore/ColumnWiseUtil.fs
            return (c >= '\u1100' && (
                // Hangul Jamo init. consonants
                c <= '\u115f' || c == '\u2329' || c == '\u232a' ||
                // CJK ... Yi
                (c >= '\u2e80' && c <= '\ua4cf' && c != '\u303f') ||
                // Hangul Syllables */
                (c >= '\uac00' && c <= '\ud7a3') ||
                // CJK Compatibility Ideographs
                (c >= '\uf900' && c <= '\ufaff') ||
                // Vertical forms
                (c >= '\ufe10' && c <= '\ufe19') ||
                // CJK Compatibility Forms
                (c >= '\ufe30' && c <= '\ufe6f') ||
                // Fullwidth Forms
                (c >= '\uff00' && c <= '\uff60') ||
                (c >= '\uffe0' && c <= '\uffe6')));
                // FIXME: handle surrogate pairs
        }

        private int getCharacterWidth(char c)
        {
            switch (c)
            {
                case '\0': return 1;
                case '\t':
                    return _view.Options.GetOptionValue<int>(DefaultOptions.TabSizeOptionId);
                default:
                    if (isNonSpacingCharacter(c)) return 0;
                    else if (isWideCharacter(c)) return 2;
                    else return 1;
            }
        }

        public SnapshotPoint? getCharAtColumn(SnapshotSpan line, int column)
        {
            ++column;

            int current = 0;
            SnapshotPoint result = line.Start;
            int charwidth = 0;
            
            if (result != line.End) charwidth = getCharacterWidth(result.GetChar());

            while (current + charwidth < column && result < line.End)
            {
                current += charwidth;
                result = result.Add(1);

                if (result < line.End)
                {
                    charwidth = getCharacterWidth(result.GetChar());
                }
            }

            if (result >= line.End) return null;
            return result;
        }

        private IEnumerable<ITextSnapshotLine> linesForSpan(SnapshotSpan span)
        {
            int firstLine = span.Snapshot.GetLineNumberFromPosition(span.Start);
            int lastLine  = span.Snapshot.GetLineNumberFromPosition(span.End);

            for (int i = firstLine; i <= lastLine; i++)
            {
                yield return span.Snapshot.GetLineFromLineNumber(i);
            }
        }

        private IEnumerable<ITagSpan<ClassificationTag>> classifyLine(ITextSnapshotLine line, ClassificationTag tag)
        {
            SnapshotPoint? point = line.Start;
            int lastColumn = 0;
            foreach (var column in _columns)
            {
                point = getCharAtColumn(new SnapshotSpan(point.Value, line.End), column - lastColumn);
                if (point == null) break;
                lastColumn = column;

                yield return new TagSpan<ClassificationTag>(new SnapshotSpan(point.Value, 1), tag);
            }
        }

        private IEnumerable<ITagSpan<ClassificationTag>> classifyLines(SnapshotSpan span, ClassificationTag tag)
        {
            return from lin in linesForSpan(span)
                   from taggedSpan in classifyLine(lin, tag)
                   select taggedSpan;
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                foreach (var taggedSpan in classifyLines(span, _tag))
                {
                    yield return taggedSpan;
                }
            }
        }
    }
    #endregion //Classifier
}
