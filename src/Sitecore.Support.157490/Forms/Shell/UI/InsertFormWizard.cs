namespace Sitecore.Support.Forms.Shell.UI
{
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Form.Core.Configuration;
    using Sitecore.Forms.Core.Data;
    using Sitecore.Globalization;
    using Sitecore.Layouts;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.WFFM.Abstractions.Dependencies;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Linq;

    public class InsertFormWizard : Sitecore.Forms.Shell.UI.InsertFormWizard
    {
        private void UpdateIDReferences(FormItem oldForm, FormItem newForm)
        {
            MethodInfo method = Assembly.Load("Sitecore.Forms.Core").GetType("Sitecore.Forms.Core.Data.FormItemSynchronizer").GetMethod("UpdateIDReferences", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, new object[] { oldForm, newForm });
        }

        private void SupportSaveForm() // method from CreateFormWizard
        {
            string goalID = this.Goals.Value;
            Item formsRoot = this.FormsRoot;
            Assert.IsNotNull(formsRoot, "forms root");
            string queryString = WebUtil.GetQueryString("la");
            Language contentLanguage = Context.ContentLanguage;
            if (!string.IsNullOrEmpty(queryString))
            {
                Language.TryParse(WebUtil.GetQueryString("la"), out contentLanguage);
            }
            Item item = this.FormsRoot.Database.GetItem(this.CreateBlankForm.Checked ? string.Empty : this.multiTree.Selected, contentLanguage);
            string name = this.EbFormName.Value;
            string copyName = ItemUtil.ProposeValidItemName(name);
            if (item != null)
            {
                Item oldForm = item;
                #region Changed code
                item = Context.Workflow.CopyItem(item, formsRoot, copyName, new ID(), true);
                #endregion
                this.UpdateIDReferences(oldForm, item);
            }
            else
            {
                if (formsRoot.Language != contentLanguage)
                {
                    formsRoot = this.FormsRoot.Database.GetItem(formsRoot.ID, contentLanguage);
                }
                item = Context.Workflow.AddItem(copyName, new TemplateID(IDs.FormTemplateID), formsRoot);
                item.Editing.BeginEdit();
                item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value = "1";
                item.Editing.EndEdit();
            }
            item.Editing.BeginEdit();
            item[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID] = name;
            item[Sitecore.Form.Core.Configuration.FieldIDs.DisplayNameFieldID] = name;
            item.Editing.EndEdit();
            this.SaveAnalytics(item, goalID);
            base.ServerProperties[this.newFormUri] = item.Uri.ToString();
            Registry.SetString("/Current_User/Dialogs//sitecore/shell/default.aspx?xmlcontrol=Forms.FormDesigner", "1250,500");
            SheerResponse.SetDialogValue(item.Uri.ToString());
        }

        protected override void SaveForm()
        {
            Item form;
            string deviceID = this.Placeholders.DeviceID;
            if (!this.InsertForm.Checked)
            {
                #region Changed code
                SupportSaveForm(); 
                #endregion
                form = Database.GetItem(ItemUri.Parse((string)base.ServerProperties[base.newFormUri]));
            }
            else
            {
                string queryString = WebUtil.GetQueryString("la");
                Language contentLanguage = Context.ContentLanguage;
                if (!string.IsNullOrEmpty(queryString))
                {
                    Language.TryParse(WebUtil.GetQueryString("la"), out contentLanguage);
                }
                form = this.FormsRoot.Database.GetItem(base.CreateBlankForm.Checked ? string.Empty : base.multiTree.Selected, contentLanguage);
            }
            if ((this.Mode != StaticSettings.DesignMode) && (this.Mode != "edit"))
            {
                Item item = Database.GetItem(ItemUri.Parse(this.GetCurrentItem().Uri.ToString()));
                LayoutDefinition definition = LayoutDefinition.Parse(LayoutField.GetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField]));
                RenderingDefinition rendering = new RenderingDefinition();
                string listValue = this.ListValue;
                ID id = StaticSettings.GetRendering(definition);
                rendering.ItemID = id.ToString();
                if (rendering.ItemID == IDs.FormInterpreterID.ToString())
                {
                    rendering.Parameters = "FormID=" + form.ID;
                }
                else
                {
                    rendering.Datasource = form.ID.ToString();
                }
                rendering.Placeholder = listValue;
                DeviceDefinition device = definition.GetDevice(deviceID);
                List<RenderingDefinition> renderings = device.GetRenderings(rendering.ItemID);
                if ((id != IDs.FormMvcInterpreterID) && renderings.Any<RenderingDefinition>(x => (((x.Parameters != null) && x.Parameters.Contains(rendering.Parameters)) || ((x.Datasource != null) && x.Datasource.Contains(form.ID.ToString())))))
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
                }
                else
                {
                    item.Editing.BeginEdit();
                    device.AddRendering(rendering);
                    if (item.Name != "__Standard Values")
                    {
                        LayoutField.SetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField], definition.ToXml());
                    }
                    else
                    {
                        item[Sitecore.FieldIDs.LayoutField] = definition.ToXml();
                    }
                    item.Editing.EndEdit();
                }
            }
            else
            {
                LayoutDefinition definition3 = LayoutDefinition.Parse(LayoutField.GetFieldValue(Database.GetItem(ItemUri.Parse(this.GetCurrentItem().Uri.ToString())).Fields[Sitecore.FieldIDs.LayoutField]));
                RenderingDefinition rendering = new RenderingDefinition();
                string str4 = this.ListValue;
                ID id2 = StaticSettings.GetRendering(definition3);
                rendering.ItemID = id2.ToString();
                rendering.Parameters = "FormID=" + form.ID;
                rendering.Datasource = form.ID.ToString();
                rendering.Placeholder = str4;
                List<RenderingDefinition> source = definition3.GetDevice(deviceID).GetRenderings(rendering.ItemID);
                if ((id2 != IDs.FormMvcInterpreterID) && source.Any<RenderingDefinition>(x => (((x.Parameters != null) && x.Parameters.Contains(rendering.Parameters)) || ((x.Datasource != null) && x.Datasource.Contains(form.ID.ToString())))))
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
                }
                else
                {
                    SheerResponse.SetDialogValue(form.ID.ToString());
                }
            }
        }
    }
}