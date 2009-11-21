using System;
using System.ComponentModel.Design.Serialization;
using System.CodeDom;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Adds serialization code for the column headers as a collection of CodeDom statements.
    /// </summary>
    internal class ImageListViewSerializer : CodeDomSerializer
    {
        #region CodeDomSerializer Overrides
        /// <summary>
        /// Deserializes the specified serialized CodeDOM object into an object.
        /// </summary>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject)
        {
            CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageListView).BaseType, typeof(CodeDomSerializer));
            return baseSerializer.Deserialize(manager, codeObject);
        }
        /// <summary>
        /// Serializes the specified object into a CodeDOM object.
        /// </summary>
        public override object Serialize(IDesignerSerializationManager manager, object value)
        {
            CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageListView).BaseType, typeof(CodeDomSerializer));
            object codeObject = baseSerializer.Serialize(manager, value);

            if (codeObject is CodeStatementCollection)
            {
                CodeStatementCollection statements = (CodeStatementCollection)codeObject;
                CodeExpression imageListViewCode = base.SerializeToExpression(manager, value);
                if (imageListViewCode != null && value is ImageListView)
                {
                    int index = 0;
                    foreach (ImageListView.ImageListViewColumnHeader column in ((ImageListView)value).Columns)
                    {
                        if (!(column.Text == column.DefaultText && 
                            column.Width == ImageListView.DefaultColumnWidth && 
                            column.DisplayIndex == index && 
                            ((index < 4) == column.Visible)))
                        {
                            CodeMethodInvokeExpression columnSetCode = new CodeMethodInvokeExpression(imageListViewCode,
                                "SetColumnHeader",
                                new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(ColumnType)), Enum.GetName(typeof(ColumnType), column.Type)),
                                new CodePrimitiveExpression(column.Text),
                                new CodePrimitiveExpression(column.Width),
                                new CodePrimitiveExpression(column.DisplayIndex),
                                new CodePrimitiveExpression(column.Visible)
                                );
                            if (column.Text == column.DefaultText)
                                columnSetCode.Parameters.RemoveAt(1);
                            statements.Add(columnSetCode);
                        }
                        index++;
                    }
                }

                return codeObject;
            }

            return base.Serialize(manager, value);
        }
        #endregion
    }
}
