using System;
using System.Linq;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Services.Models;
using Orchard.Layouts.Framework.Elements;
using Orchard.Tokens;

namespace Orchard.DynamicForms.Tokens {
    public class FormTokens : Component, ITokenProvider {
        private readonly IContentManager _contentManager;
        
        public FormTokens(IContentManager contentManager) {
            _contentManager = contentManager;
        }
        public void Describe(DescribeContext context) {
            context.For("FormSubmission", T("Dynamic Form submission"), T("Dynamic Form Submission tokens for use in workflows handling the Dynamic Form Submitted event."))
                .Token("Field:*", T("Field:<field name>"), T("The posted field value to access."), "Text")
                .Token("IsValid:*", T("IsValid:<field name>"), T("The posted field validation status."))
                .Token("FormName", T("FormName"), T("The posted form's name."));

            context.For("Element", T("Element"), T("Form Element tokens to get the content that is going to be edited through a dynamic form."))
                .Token("ContentToEdit", T("ContentToEdit"), T("The content item to edit through a form."));            

            context.For("Content", T("Contet Items"), T("Content Items"))
                .Token("UrlForEditWithForm:*,", T("UrlForEditWithForm:<layoutRoute>|<formName>"), T("Url to edit the content through a dynamic form."))
                .Token("UrlForDeleteWithForm:*,", T("UrlForDeleteWithForm:<layoutRoute>|<formName>"), T("Url to delete the content through a dynamic form."))
                ;
        }

        public void Evaluate(EvaluateContext context) {
            context.For<FormSubmissionTokenContext>("FormSubmission")
                .Token(token => token.StartsWith("Field:", StringComparison.OrdinalIgnoreCase) ? token.Substring("Field:".Length) : null, GetFieldValue)
                .Chain(FilterChainParam, "Text", GetFieldValue)
                .Token(token => token.StartsWith("IsValid:", StringComparison.OrdinalIgnoreCase) ? token.Substring("IsValid:".Length) : null, GetFieldValidationStatus)
                .Token("FormName", GetFormName)
                .Chain("FormName", "Text", GetFormName)
                .Token("ContentToEdit", s => GetContentToEdit(s.Form))
                .Chain("ContentToEdit", "Content", s => GetContentToEdit(s.Form));

            context.For<Element>("Element")
                .Token("ContentToEdit", GetContentToEdit)
                .Chain("ContentToEdit", "Content", GetContentToEdit);

            context.For<IContent>("Content")
                .Token(token => token.StartsWith("UrlForEditWithForm:", StringComparison.OrdinalIgnoreCase) ? token.Substring("UrlForEditWithForm:".Length) : null
                , (token, content) => GetEditWithFormUrl(token, content))
                .Chain(token => (token.StartsWith("UrlForEditWithForm:", StringComparison.OrdinalIgnoreCase) ? new Tuple<string, string>("UrlForEditWithForm", token) : null), "Url", (token, content) => GetEditWithFormUrl(token, content))
                .Token(token => token.StartsWith("UrlForDeleteWithForm:", StringComparison.OrdinalIgnoreCase) ? token.Substring("UrlForDeleteWithForm:".Length) : null
                , (token, content) => GetDeleteWithFormUrl(token, content))
                .Chain(token => (token.StartsWith("UrlForDeleteWithForm:", StringComparison.OrdinalIgnoreCase)? new Tuple<string,string>("UrlForDeleteWithForm", token):null) , "Url",(token, content) => GetDeleteWithFormUrl(token,content))
            ;
        }

        private object GetContentToEdit(Element element) {
            Form form = element as Form;
            if (form != null)
                return form.ContentItemToEdit;
            var formElement = element as FormElement;
            if (formElement != null)
                return formElement.Form.ContentItemToEdit;
            while (element!=null) {
                element = element.Container;
                form = element as Form;
                if (form != null)
                    return form.ContentItemToEdit;
                formElement = element as FormElement;
                if (formElement != null)
                    return formElement.Form.ContentItemToEdit;                
            }
            return null;
        }

        private static Tuple<string, string> FilterChainParam(string token) {
            int tokenLength = "Field:".Length;
            int chainIndex = token.IndexOf('.');
            if (token.StartsWith("Field:", StringComparison.OrdinalIgnoreCase) && chainIndex > tokenLength)
                return new Tuple<string, string>(token.Substring(tokenLength, chainIndex - tokenLength), token.Substring(chainIndex + 1));
            else
                return null;
        }

        private string GetFieldValue(string fieldName, FormSubmissionTokenContext context) {
            return context.PostedValues[fieldName];
        }
        private string GetFormName(FormSubmissionTokenContext context)
        {
            return context.Form.Name;
        }
        private object GetFieldValidationStatus(string fieldName, FormSubmissionTokenContext context) {
            return context.ModelState.IsValidField(fieldName);
        }
        private string GetEditWithFormUrl(string parammeters, IContent context) {
            var paramArray = parammeters.Split('|');
            string layoutRoute = paramArray[0];
            string formName = paramArray[1];
            string returnurl = "";
            return string.Format("/{0}?{1}Form_edit={2}", layoutRoute, formName, context.ContentItem.Id);
        }
        private string GetDeleteWithFormUrl(string parammeters, IContent context) {
            var paramArray = parammeters.Split('|');
            string layoutRoute = paramArray[0];
            string formName = paramArray[1];
            var layoutContent = _contentManager.Query<AutoroutePart>().Where<AutoroutePartRecord>(a => a.DisplayAlias == layoutRoute).Slice(0, 1).First().ContentItem;
            return string.Format("/orchard.dynamicforms/Form/Command?layoutContentId={0}&formName={1}&command=Delete&currentContentId={2}", layoutContent.Content.Id, formName, context.ContentItem.Id);
        }
    }
}