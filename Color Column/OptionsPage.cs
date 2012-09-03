using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace ColorColumn
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class OptionsPage : DialogPage, INotifyPropertyChanged {
        private const string defaultColorColumn = "80,120";

        public event PropertyChangedEventHandler PropertyChanged;

        void raisePropertyChanged() {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(null));
            }
        }

        [Category("Color Columns")]
        [DisplayName("Color Columns")]
        [Description("The columns that will be highlighted, as a comma-separated string")]
        [DefaultValue(defaultColorColumn)]
        public string ColorColumns { get; set; }

        [Category("Color Columns")]
        [DisplayName("Cursor Column")]
        [Description("Whether the column of the cursor must be highlighted")]
        [DefaultValue(false)]
        public bool CursorColumn { get; set; }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
 	        base.OnApply(e);
            raisePropertyChanged();
        }

        [Browsable(false)]
        public IEnumerable<int> ParsedColumns
        {
            get
            {
                if (ColorColumns == null)
                {
                    ColorColumns = defaultColorColumn;
                }

                foreach (var item in ColorColumns.Split(','))
                {
                    int v;
                    if (int.TryParse(item, out v))
                    {
                        yield return v;
                    }
                }
            }
        }
    }
}