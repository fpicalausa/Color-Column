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
    /// <summary>
    /// This class causes a classifier to be added to the set of classifiers. Since 
    /// the content type is set to "text", this classifier applies to all text files
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("code")]
    internal class ColorColumnTextClassifierProvider : IClassifierProvider
    {
        /// <summary>
        /// Import the classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

        [Import]
        internal SVsServiceProvider serviceProvider = null; // Set via MEF

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            IVsPackage package; 
            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            Marshal.ThrowExceptionForHR(shell.LoadPackage(new Guid(GuidList.colorColumnPkgString), out package));

            return buffer.Properties.GetOrCreateSingletonProperty<ColorColumnTextClassifier>(
                delegate { return new ColorColumnTextClassifier(
                    ClassificationRegistry,
                    ((ColorColumnPackage)package).Settings,
                    buffer
                    ); });
        }
    }
    #endregion //provider def

    #region Classifier
    /// <summary>
    /// Classifier that classifies all text as an instance of the OrinaryClassifierType
    /// </summary>
    class ColorColumnTextClassifier : IClassifier
    {
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        IClassificationType _classificationType;
        OptionsPage         _options;
        List<int>           _columns;
        ITextBuffer         _buffer;

        internal ColorColumnTextClassifier(IClassificationTypeRegistryService registry, OptionsPage options, ITextBuffer buffer)
        {
            _classificationType = registry.GetClassificationType("Color Column Text");
            _options = options;
            _columns = new List<int>();
            _buffer = buffer;

            _options.PropertyChanged += _options_PropertyChanged;
            updateColumns();
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

                //TODO: Keep the old columns value, and reclassify only the 
                // lines that need to be reclassified.
                tmp(this, new ClassificationChangedEventArgs(
                    new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
        }

        private void classifyLine(ITextSnapshotLine line, List<ClassificationSpan> classifications)
        {
            foreach (var column in _columns)
            {
                if (column >= line.Length) break;

                classifications.Add(new ClassificationSpan(new SnapshotSpan(line.Start.Add(column), 1), _classificationType));
            }
        }

        /// <summary>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </summary>
        /// <param name="trackingSpan">The span currently being classified</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            //create a list to hold the results
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();

            ITextSnapshot snapshot = span.Snapshot;

            int start = snapshot.GetLineNumberFromPosition(span.Start);
            int end = snapshot.GetLineNumberFromPosition(span.End) + 1;

            for (int i = start; i != end; i++)
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                if (line.Length >= _columns[0])
                {
                    classifyLine(line, classifications);
                }
            }

            return classifications;
        }

    }
    #endregion //Classifier
}
