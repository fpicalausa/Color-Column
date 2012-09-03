using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace ColorColumn
{
    #region Adornment Factory
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ColorColumnAdornmentFactory : IWpfTextViewCreationListener
    {
        private IEditorFormatMapService formatMapService;
        private OptionsPage settings;

        /// <summary>
        /// Defines the adornment layer for the color columns adornment. This layer should stay in the background,
        /// behind the text.
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ColorColumn")]
        [Order(Before = PredefinedAdornmentLayers.CurrentLineHighlighter)]

        // MEF
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        // MEF
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), ImportingConstructor]
        internal ColorColumnAdornmentFactory(IEditorFormatMapService formatMapService, SVsServiceProvider serviceProvider)
        {
            this.formatMapService = formatMapService;

            IVsPackage package; 

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            Marshal.ThrowExceptionForHR(shell.LoadPackage(new Guid(GuidList.colorColumnPkgString), out package));

            this.settings = ((ColorColumnPackage)package).Settings;
        }

        /// <summary>
        /// Instantiates a ColorColumn manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            new ColorColumn(textView, formatMapService.GetEditorFormatMap(textView), settings);
        }
    }

    #endregion //Adornment Factory
}
