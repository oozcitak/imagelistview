using System.Drawing.Design;
using System.Windows.Forms;
using System.ComponentModel;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Displays a open file dialog box on the property grid.
    /// </summary>
    internal class OpenFileDialogEditor : UITypeEditor
    {
        /// <summary>
        /// Gets the edit style.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The edit style.</returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context != null && context.Instance != null)
                return UITypeEditorEditStyle.Modal;

            return UITypeEditorEditStyle.None;
        }

        //[RefreshProperties(RefreshProperties.All)]
        /// <summary>
        /// Edits the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="value">The value.</param>
        /// <returns>New value.</returns>
        public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
            if (provider != null && context != null && context.Instance != null)
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = "Select " + context.PropertyDescriptor.DisplayName;
                    dlg.FileName = value.ToString();
                    dlg.Filter = "All image files (*.bmp, *.gif, *.jpg, *.jepg, *.jpe, *.jif, *.png, *.tif, *.tiff, *.tga)|" + 
                        "*.bmp;*.gif;*.jpg;*.jepg;*.jpe;*.jif;*.png;*.tif;*.tiff;*.tga|" + 
                        "BMP (*.bmp)|*.bmp|GIF (*.gif)|*.gif|JPEG (*.jpg, *.jepg, *.jpe, *.jif)|*.jpg;*.jepg;*.jpe;*.jif|" + 
                        "PNG (*.png)|*.png|TIFF (*.tif, *.tiff)|*.tif;*.tiff|TGA (*.tga)|*.tga|All files (*.*)|*.*";

                    if (dlg.ShowDialog() == DialogResult.OK)
                        value = dlg.FileName;

                    return value;
                }
            }

            return base.EditValue(provider, value);
        }
    }
}
