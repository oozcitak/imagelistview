using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the designer of the image list view.
    /// </summary>
    internal class ImageListViewDesigner : ControlDesigner
    {
        #region Member Variables
        DesignerActionListCollection actionLists;
        #endregion

        #region ControlDesigner Overrides
        /// <summary>
        /// Gets the design-time action lists supported by the component associated with the designer.
        /// </summary>
        public override DesignerActionListCollection ActionLists
        {
            get
            {
                if (null == actionLists)
                {
                    actionLists = base.ActionLists;
                    actionLists.Add(new ImageListViewActionLists(this.Component));
                }
                return actionLists;
            }
        }
        #endregion
    }

    /// <summary>
    /// Defines smart tag entries for the image list view.
    /// </summary>
    internal class ImageListViewActionLists : DesignerActionList, IServiceProvider, IWindowsFormsEditorService, ITypeDescriptorContext
    {
        #region Member Variables
        private ImageListView imageListView;
        private DesignerActionUIService designerService;

        private PropertyDescriptor property;
        #endregion

        #region Constructor
        public ImageListViewActionLists(IComponent component)
            : base(component)
        {
            imageListView = (ImageListView)component;
            designerService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Sets the specified ImageListView property.
        /// </summary>
        /// <param name="propName">Name of the member property.</param>
        /// <param name="value">New value of the property.</param>
        private void SetProperty(String propName, object value)
        {
            PropertyDescriptor prop;
            prop = TypeDescriptor.GetProperties(imageListView)[propName];
            if (prop == null)
                throw new ArgumentException("Unknown property.", propName);
            else
                prop.SetValue(imageListView, value);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the sort column of the designed ImageListView.
        /// </summary>
        public ColumnType SortColumn
        {
            get { return imageListView.SortColumn; }
            set { SetProperty("SortColumn", value); }
        }
        /// <summary>
        /// Gets or sets the sort oerder of the designed ImageListView.
        /// </summary>
        public SortOrder SortOrder
        {
            get { return imageListView.SortOrder; }
            set { SetProperty("SortOrder", value); }
        }
        /// <summary>
        /// Gets or sets the view mode of the designed ImageListView.
        /// </summary>
        public View View
        {
            get { return imageListView.View; }
            set { SetProperty("View", value); }
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Invokes the editor for the columns of the designed ImageListView.
        /// </summary>
        public void EditColumns()
        {
            property = TypeDescriptor.GetProperties(imageListView)["Columns"];
            UITypeEditor editor = (UITypeEditor)property.GetEditor(typeof(UITypeEditor));
            object value = property.GetValue(imageListView);
            value = editor.EditValue(this, this, value);
            imageListView.SetColumnsInternal((ImageListView.ImageListViewColumnHeaderCollection)value);
            designerService.Refresh(Component);
        }
        #endregion

        #region DesignerActionList Overrides
        /// <summary>
        /// Returns the collection of <see cref="T:System.ComponentModel.Design.DesignerActionItem"/> objects contained in the list.
        /// </summary>
        public override DesignerActionItemCollection GetSortedActionItems()
        {
            DesignerActionItemCollection items = new DesignerActionItemCollection();

            items.Add(new DesignerActionMethodItem(this, "EditColumns", "Edit Columns", true));

            items.Add(new DesignerActionPropertyItem("View", "View"));
            items.Add(new DesignerActionPropertyItem("SortColumn", "SortColumn"));
            items.Add(new DesignerActionPropertyItem("SortOrder", "SortOrder"));

            return items;
        }
        #endregion

        #region IServiceProvider Members
        /// <summary>
        /// Returns an object that represents a service provided by the component associated with the <see cref="T:System.ComponentModel.Design.DesignerActionList"/>.
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(IWindowsFormsEditorService)))
            {
                return this;
            }
            return GetService(serviceType);
        }
        #endregion

        #region IWindowsFormsEditorService Members
        /// <summary>
        /// Closes any previously opened drop down control area.
        /// </summary>
        void IWindowsFormsEditorService.CloseDropDown()
        {
            throw new NotSupportedException("Only modal dialogs are supported.");
        }
        /// <summary>
        /// Displays the specified control in a drop down area below a value field of the property grid that provides this service.
        /// </summary>
        void IWindowsFormsEditorService.DropDownControl(Control control)
        {
            throw new NotSupportedException("Only modal dialogs are supported.");
        }
        /// <summary>
        /// Shows the specified <see cref="T:System.Windows.Forms.Form"/>.
        /// </summary>
        DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog)
        {
            return (dialog.ShowDialog());
        }
        #endregion

        #region ITypeDescriptorContext Members
        /// <summary>
        /// Gets the container representing this <see cref="T:System.ComponentModel.TypeDescriptor"/> request.
        /// </summary>
        IContainer ITypeDescriptorContext.Container
        {
            get { return null; }
        }
        /// <summary>
        /// Gets the object that is connected with this type descriptor request.
        /// </summary>
        object ITypeDescriptorContext.Instance
        {
            get { return imageListView; }
        }
        /// <summary>
        /// Raises the <see cref="E:System.ComponentModel.Design.IComponentChangeService.ComponentChanged"/> event.
        /// </summary>
        void ITypeDescriptorContext.OnComponentChanged()
        {
            ;
        }
        /// <summary>
        /// Raises the <see cref="E:System.ComponentModel.Design.IComponentChangeService.ComponentChanging"/> event.
        /// </summary>
        bool ITypeDescriptorContext.OnComponentChanging()
        {
            return true;
        }
        /// <summary>
        /// Gets the <see cref="T:System.ComponentModel.PropertyDescriptor"/> that is associated with the given context item.
        /// </summary>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
        {
            get { return property; }
        }
        #endregion
    }
}
