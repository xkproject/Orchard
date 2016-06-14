using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Helpers;
using Orchard.DynamicForms.Services;
using Orchard.Layouts.Services;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Html;
using Orchard.Security;
using Orchard.Services;
using Orchard.Tokens;
using Orchard.UI.Notify;
using IController = Orchard.DynamicForms.Services.IController;

namespace Orchard.DynamicForms.Controllers {
    public class FormController : Controller, IController, IUpdateModel {
        private readonly INotifier _notifier;
        private readonly ILayoutManager _layoutManager;
        private readonly IFormService _formService;
        private readonly ITokenizer _tokenizer;
        private readonly IClock _clock;
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public FormController(
            INotifier notifier, 
            ILayoutManager layoutManager, 
            IFormService formService, 
            ITokenizer tokenizer,
            IClock clock,
            IContentManager contentManager,
            IAuthorizationService authorizationService,
            IAuthenticationService authenticationService,
            IContentDefinitionManager contentDefinitionManager) {

            _notifier = notifier;
            _layoutManager = layoutManager;
            _formService = formService;
            _tokenizer = tokenizer;
            _clock = clock;
            _contentManager = contentManager;
            _authorizationService = authorizationService;
            _authenticationService = authenticationService;
            _contentDefinitionManager = contentDefinitionManager;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        [HttpPost, ActionName("Submit")]
        [ValidateInput(false)]
        public ActionResult Submit(int contentId, string formName) {
            int contentItemIdToEdit = 0;
            string returnUrl = Request.Form.Get("returnUrl");
            int.TryParse(Request.Form.Get("contentIdToEdit"), out contentItemIdToEdit);
            var accessType = contentItemIdToEdit > 0 ? ContentAccessType.ForEdit : ContentAccessType.ForAdd;
            var urlReferrer = Request.UrlReferrer != null ? Request.UrlReferrer.PathAndQuery : "~/";            
            
            var layoutPart = _layoutManager.GetLayout(contentId);
            Form form = _formService.GetAuthorizedForm(layoutPart, formName, accessType);
            if (form != null)
                form.ContentItemToEdit = _formService.GetAuthorizedContentIdToEdit( layoutPart.ContentItem, form, contentItemIdToEdit, accessType);

            if (form == null || (accessType == ContentAccessType.ForEdit && form.ContentItemToEdit == null)) {
                Logger.Warning("Insufficient permissions for submitting the specified form \"{0}\".", formName);
                return new HttpNotFoundResult();
            }
            var values = _formService.SubmitForm(layoutPart, form, ValueProvider, ModelState, this);
            this.TransferFormSubmission(form, values);

            if (!ModelState.IsValid) {
                // We need a way to inform the output cache filter to not cache the upcoming request.
                var epoch = new DateTime(2014, DateTimeKind.Utc).Ticks;
                var refresh = _clock.UtcNow.Ticks - epoch;
                var query = HttpUtility.ParseQueryString(Request.UrlReferrer.Query);
                query[HttpUtility.UrlEncode(formName + "Form_edit")] = contentItemIdToEdit.ToString();
                query["__r"] = refresh.ToString();
                return Redirect(Request.UrlReferrer.LocalPath + "?" + query.ToQueryString());                
            }

            if(Response.IsRequestBeingRedirected)
                return new EmptyResult();

            if (!String.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            if (!String.IsNullOrWhiteSpace(form.RedirectUrl)) 
                return Redirect(_tokenizer.Replace(form.RedirectUrl, new { Content = layoutPart.ContentItem }));

            //I don't know how to get elegantly the id fo a contentitem added to navigate to it if permissions allow it;
            var user = _authenticationService.GetAuthenticatedUser();
            if (Request.UrlReferrer != null && _authorizationService.TryCheckAccess(Permissions.SubmitAnyFormForModifyOwnData, user, layoutPart.ContentItem, form.Name)) {
                var query = HttpUtility.ParseQueryString(Request.UrlReferrer.Query);
                query[HttpUtility.UrlEncode(formName + "Form_edit")] = contentItemIdToEdit.ToString();
                return Redirect(Request.UrlReferrer.LocalPath + "?" + query.ToQueryString());
            }
            return Redirect(urlReferrer);
        }

        [HttpGet]
        public ActionResult Command(int layoutContentId, string formName, string command, int currentContentId=0) {
            var urlReferrer = ((Request.UrlReferrer != null) ? Request.UrlReferrer.PathAndQuery : "~/");

            var accessType = ContentAccessType.ForRead;
            if (string.Compare(command, DynamicFormCommand.Delete.ToString(), true) == 0)
                accessType = ContentAccessType.ForDelete;
            else if (string.Compare(command, DynamicFormCommand.New.ToString(), true) == 0)
                accessType = ContentAccessType.ForAdd;

            var layoutPart = _layoutManager.GetLayout(layoutContentId);
            var form = _formService.GetAuthorizedForm(layoutPart, formName, accessType);
            
            if (form == null) {
                Logger.Warning("Insufficient permissions for submitting the specified form \"{0}\".", formName);
                _notifier.Warning(T("The specified form \"{0}\" could not be found.", formName));
                return new HttpNotFoundResult();
            }
            
            if (form.CreateContent != true || String.IsNullOrWhiteSpace(form.FormBindingContentType)) {
                _notifier.Warning(T("The form \"{0}\" cannot load invalid data", formName));
                Logger.Warning(String.Format("Attempting to display contem item s in form \"{1}\" not binded to a content type.", formName));
                return Redirect(urlReferrer);
            }

            form.ContentItemToEdit = _formService.GetAuthorizedContentIdToEdit(layoutPart.ContentItem, form, currentContentId, accessType);
            
            int contentItemId = 0;
            if (!_formService.TryGetNextContentIdAfterApplyDynamicFormCommand(layoutPart, form, command, layoutPart.ContentItem, out contentItemId))
                return Redirect(urlReferrer);

            if (string.Compare(DynamicFormCommand.Delete.ToString(),command)==0)
                _contentManager.Remove(form.ContentItemToEdit.ContentItem);

            var query = new NameValueCollection();
            var contentLocalPath = (new UrlHelper(Request.RequestContext)).ItemDisplayUrl(layoutPart.ContentItem);
            if (Request.UrlReferrer.LocalPath == contentLocalPath) {
                query = HttpUtility.ParseQueryString(Request.UrlReferrer.Query);
                query[HttpUtility.UrlEncode(formName + "Form_edit")] = contentItemId.ToString();
                urlReferrer = contentLocalPath + "?" + query.ToQueryString();
            }
            return Redirect(urlReferrer);            
            
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        void IUpdateModel.AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.Text);
        }
    }
}