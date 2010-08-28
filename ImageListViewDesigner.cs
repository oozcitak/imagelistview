// ImageListView - A listview control for image files
// Copyright (C) 2009 Ozgur Ozcitak
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ozgur Ozcitak (ozcitak@yahoo.com)

using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Windows.Forms.Design.Behavior;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents an ImageListViewITem on the designer.
    /// </summary>
    internal class ItemGlyph : Glyph, IDisposable
    {
        #region Member Variables
        private BehaviorService mBehaviorService;
        private ImageListView mImageListView;
        private int mIndex;
        private ImageListViewItem mItem;
        private Point offset;
        internal Rectangle clipBounds;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the bounds of the <see cref="T:System.Windows.Forms.Design.Behavior.Glyph"/>.
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                // Glyph coordinates are in adorner window coordinates, so we must map
                // using the behavior service.
                Rectangle bounds = mImageListView.layoutManager.GetItemBounds(mIndex);
                Point aPt = mBehaviorService.MapAdornerWindowPoint(mImageListView.Handle, bounds.Location);
                offset = new Point(aPt.X - bounds.X, aPt.Y - bounds.Y);
                bounds.Offset(offset);

                return bounds;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ItemGylph class.
        /// </summary>
        /// <param name="behaviorService">The behavior service of the designer.</param>
        /// <param name="owner">The owner control.</param>
        /// <param name="text">The text of the item.</param>
        /// <param name="index">Item index.</param>
        public ItemGlyph(BehaviorService behaviorService, ImageListView owner, string text, int index)
            : base(null)
        {
            mBehaviorService = behaviorService;
            mImageListView = owner;
            mItem = new ImageListViewItem();
            mItem.Text = text;
            mItem.mImageListView = mImageListView;
            mItem.Tag = null;
            mIndex = index;
        }
        #endregion

        #region Glyph Overrides
        /// <summary>
        /// Provides hit test logic.
        /// </summary>
        /// <param name="p">A point to hit-test.</param>
        /// <returns>
        /// A <see cref="T:System.Windows.Forms.Cursor"/> if the <see cref="T:System.Windows.Forms.Design.Behavior.Glyph"/> 
        /// is associated with <paramref name="p"/>; otherwise, null.
        /// </returns>
        public override Cursor GetHitTest(Point p)
        {
            return null;
        }
        /// <summary>
        /// Provides paint logic.
        /// </summary>
        /// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
        public override void Paint(PaintEventArgs pe)
        {
            if (mItem.Tag == null)
            {
                int c = 0;
                foreach (ImageListView.ImageListViewColumnHeader column in mImageListView.Columns)
                {
                    if (column.Type == ColumnType.Custom)
                    {
                        mItem.AddSubItemText(column.columnID);
                        c++;
                    }
                }
                mItem.Tag = c.ToString();
            }

            mImageListView.layoutManager.Update(true);
            Rectangle itemArea = mImageListView.layoutManager.ItemAreaBounds;
            itemArea.Offset(offset);
            Rectangle clip = Rectangle.Intersect(Bounds, itemArea);
            Rectangle overlay = clipBounds;
            overlay.Offset(offset);
            clip = Rectangle.Intersect(clip, overlay);
            pe.Graphics.SetClip(clip);
            mImageListView.mRenderer.DrawItem(pe.Graphics, mItem, ItemState.None, Bounds);

            if (mImageListView.ShowCheckBoxes)
            {
                Rectangle bounds = mImageListView.layoutManager.GetCheckBoxBounds(mIndex);
                bounds.Offset(offset);
                mImageListView.mRenderer.DrawCheckBox(pe.Graphics, mItem, bounds);
            }
            if (mImageListView.ShowFileIcons)
            {
                Rectangle bounds = mImageListView.layoutManager.GetIconBounds(mIndex);
                bounds.Offset(offset);
                mImageListView.mRenderer.DrawFileIcon(pe.Graphics, mItem, bounds);
            }
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (mItem != null)
                mItem.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// Represents the designer of the image list view.
    /// </summary>
    internal class ImageListViewDesigner : ControlDesigner
    {
        #region Member Variables
        private DesignerActionListCollection actionLists = null;
        private Adorner adorner;
        private ImageListView imageListView;
        #endregion

        #region Add/Remove Glyphs on Initialize/Dispose
        /// <summary>
        /// Initializes the designer with the specified component.
        /// </summary>
        /// <param name="component">The <see cref="T:System.ComponentModel.IComponent"/> 
        /// to associate the designer with. This component must always be an instance of, 
        /// or derive from, <see cref="T:System.Windows.Forms.Control"/>.</param>
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            imageListView = (ImageListView)this.Control;
            // Add the custom glyphs
            adorner = new Adorner();
            BehaviorService.Adorners.Add(adorner);
            adorner.Glyphs.Add(new ItemGlyph(BehaviorService, imageListView, "Item 1", 0));
            adorner.Glyphs.Add(new ItemGlyph(BehaviorService, imageListView, "Item 2", 1));
            adorner.Glyphs.Add(new ItemGlyph(BehaviorService, imageListView, "Item 3", 2));
        }
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Design.ControlDesigner"/> 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; 
        /// false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && adorner != null)
            {
                foreach (Glyph g in adorner.Glyphs)
                {
                    if (g is ItemGlyph)
                        ((ItemGlyph)g).Dispose();
                }
                BehaviorService b = BehaviorService;
                if (b != null)
                {
                    b.Adorners.Remove(adorner);
                }
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Designer Action Lists
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

        #region Paint Adornments
        /// <summary>
        /// Receives a call when the control that the designer is managing has painted 
        /// its surface so the designer can paint any additional adornments on top of the control.
        /// </summary>
        /// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> the designer 
        /// can use to draw on the control.</param>
        protected override void OnPaintAdornments(PaintEventArgs pe)
        {
            base.OnPaintAdornments(pe);

            foreach (Glyph g in adorner.Glyphs)
            {
                if (g is ItemGlyph)
                    ((ItemGlyph)g).clipBounds = pe.ClipRectangle;
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
        /// <summary>
        /// Initializes a new instance of the ImageListViewActionLists class.
        /// </summary>
        /// <param name="component">A component related to the DesignerActionList.</param>
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
        public int SortColumn
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
            // TODO: Column editing cannot be undone in the designer.
            property = TypeDescriptor.GetProperties(imageListView)["Columns"];
            UITypeEditor editor = (UITypeEditor)property.GetEditor(typeof(UITypeEditor));
            object value = imageListView.Columns;// property.GetValue(imageListView);
            value = editor.EditValue(this, this, value);
            SetProperty("Columns", value);
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
        /// Returns an object that represents a service provided by the component 
        /// associated with the <see cref="T:System.ComponentModel.Design.DesignerActionList"/>.
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
        /// Displays the specified control in a drop down area below a value 
        /// field of the property grid that provides this service.
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
        /// Gets the container representing this 
        /// <see cref="T:System.ComponentModel.TypeDescriptor"/> request.
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
        /// Gets the <see cref="T:System.ComponentModel.PropertyDescriptor"/> 
        /// that is associated with the given context item.
        /// </summary>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
        {
            get { return property; }
        }
        #endregion
    }
}
