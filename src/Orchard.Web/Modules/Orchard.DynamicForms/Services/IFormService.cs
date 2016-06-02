﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Web.Mvc;
using Orchard.Collections;
using Orchard.ContentManagement;
using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Models;
using Orchard.DynamicForms.Services.Models;
using Orchard.Layouts.Models;

namespace Orchard.DynamicForms.Services {
    public enum ContentAccessType { ForAdd, ForEdit, ForDelete, ForRead };

    public interface IFormService : IDependency {       
        Form GetAuthorizedForm(LayoutPart layoutPart, string formName, ContentAccessType accessType);
        IContent GetAuthorizedContentIdToEdit(IContent layoutContentItem, Form form, int contenItemIdToEdit, ContentAccessType contentAccessType);
        Form FindForm(LayoutPart layoutPart, string formName = null);
        IEnumerable<Form> GetAllForms(LayoutPart layoutPart);
        IEnumerable<FormElement> GetFormElements(Form form);
        IEnumerable<string> GetFormElementNames(Form form);
        NameValueCollection SubmitForm(IContent content, Form form, IValueProvider valueProvider, ModelStateDictionary modelState, IUpdateModel updater);
        Submission CreateSubmission(string formName, NameValueCollection values);
        Submission CreateSubmission(Submission submission);
        Submission GetSubmission(int id);
        IPageOfItems<Submission> GetSubmissions(string formName = null, int? skip = null, int? take = null);
        void DeleteSubmission(Submission submission);
        int DeleteSubmissions(IEnumerable<int> submissionIds);
        void ReadElementValues(FormElement element, ReadElementValuesContext context);
        NameValueCollection ReadElementValues(Form form, IValueProvider valueProvider);
        DataTable GenerateDataTable(IEnumerable<Submission> submissions);
        void CreateContentItem(IContent content, Form form, IValueProvider valueProvider);
        void UpdateContentItem(IContent content, Form form, IValueProvider valueProvider);        
        NameValueCollection GetValuesFromContentItem(Form form);
        IEnumerable<IElementValidator> GetValidators<TElement>() where TElement : FormElement;
        IEnumerable<IElementValidator> GetValidators(FormElement element);
        IEnumerable<IElementValidator> GetValidators(Type elementType);
        IEnumerable<IElementValidator> GetValidators();
        void RegisterClientValidationAttributes(FormElement element, RegisterClientValidationAttributesContext context);

        bool TryGetNextContentIdAfterApplyDynamicFormCommand(LayoutPart layoutPart, Form form, string command, IContent currentContent, out int contentId);
    }
}