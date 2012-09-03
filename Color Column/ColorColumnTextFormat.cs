using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace ColorColumn
{
    #region Format definition
    /// <summary>
    /// Defines an editor format for highlighted columns
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Color Column Text")]
    [Name(ColorColumnTextFormat.name)]
    [DisplayName("Color columns")]
    [UserVisible(true)] //this should be visible to the end user
    [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
    internal sealed class ColorColumnTextFormat : ClassificationFormatDefinition
    {
        public const string name = "ColorColumnTextFormat";
        public static Color defaultBackgroundColor = Color.FromRgb(0x30, 0x30, 0x30);

        /// <summary>
        /// Defines the visual format for the "EditorClassifier1" classification type
        /// </summary>
        public ColorColumnTextFormat()
        {
            this.DisplayName = "Color column text";
            this.BackgroundColor = defaultBackgroundColor;
            this.ForegroundColor = null;
            this.BackgroundCustomizable = true;
            this.ForegroundCustomizable = true;
        }
    }

    /// <summary>
    /// Defines an editor format for highlighting the current cursor column
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Cursor Column")]
    [Name(CursorColumnFormat.name)]
    [DisplayName("Cursor column")]
    [UserVisible(true)] //this should be visible to the end user
    [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
    internal sealed class CursorColumnFormat : EditorFormatDefinition
    {
        public const string name = "CursorColumnFormat";
        public static Color defaultBackgroundColor = Color.FromRgb(0x30, 0x30, 0x30);

        /// <summary>
        /// Defines the visual format for the "EditorClassifier1" classification type
        /// </summary>
        public CursorColumnFormat()
        {
            this.DisplayName = "Cursor column text";
            this.BackgroundColor = defaultBackgroundColor;
            this.ForegroundCustomizable = false;
            this.BackgroundCustomizable = true;
        }
    }
    #endregion //Format definition
}
