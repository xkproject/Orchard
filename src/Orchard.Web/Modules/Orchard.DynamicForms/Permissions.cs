using System.Collections.Generic;
using Orchard.Environment.Extensions.Models;
using Orchard.Security.Permissions;

namespace Orchard.DynamicForms {
    public class Permissions : IPermissionProvider {
        public static readonly Permission ManageForms = new Permission { Description = "Manage custom form", Name = "ManageForms" };
        public static readonly Permission SubmitAnyForm = new Permission { Description = "Submit any form", Name = "SubmitAny" };
        public static readonly Permission SubmitAnyFormForModifyData = new Permission { Description = "Submit any form for modify data own by others", Name = "SubmitAnyForModify" };
        public static readonly Permission SubmitAnyFormForModifyOwnData = new Permission { Description = "Submit any form for modify own data", Name = "SubmitAnyForModifyOwn", ImpliedBy = new[] { SubmitAnyFormForModifyData } };
        public static readonly Permission SubmitAnyFormForDeleteData = new Permission { Description = "Submit any form for delete data own by others", Name = "SubmitAnyForDelete" };
        public static readonly Permission SubmitAnyFormForDeleteOwnData = new Permission { Description = "Submit any form for delete own data", Name = "SubmitAnyForDeleteOwn", ImpliedBy = new[] { SubmitAnyFormForDeleteData } };
        public static readonly Permission ShowAnyForm = new Permission { Description = "Show any form", Name = "ShowAnyForm", ImpliedBy = new[] { SubmitAnyFormForModifyData } };
        public static readonly Permission ShowAnyFormWithOwnData = new Permission { Description = "Show any form with own data", Name = "ShowAnyFormWithOwnData", ImpliedBy = new[] { ShowAnyForm, SubmitAnyFormForModifyOwnData } };

        public virtual Feature Feature { get; set; }
        
        public IEnumerable<Permission> GetPermissions() {
            yield return SubmitAnyForm;
            yield return SubmitAnyFormForModifyData;
            yield return SubmitAnyFormForModifyOwnData;
            yield return SubmitAnyFormForDeleteData;
            yield return SubmitAnyFormForDeleteOwnData;
            yield return ShowAnyForm;
            yield return ShowAnyFormWithOwnData;
            yield return ManageForms;            
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new[] { ShowAnyForm, SubmitAnyForm, ManageForms }
                },
                new PermissionStereotype {
                    Name = "Editor",
                    Permissions = new[] { ShowAnyForm, SubmitAnyForm }
                },
                new PermissionStereotype {
                    Name = "Moderator",
                    Permissions = new[] { ShowAnyForm, SubmitAnyForm }
                },
                new PermissionStereotype {
                    Name = "Author",
                    Permissions = new[] { ShowAnyForm, SubmitAnyForm }
                },
                new PermissionStereotype {
                    Name = "Contributor",
                    Permissions = new[] { ShowAnyForm, SubmitAnyForm }
                }
            };
        }

        public static Permission GetOwnerVariation(Permission permission) {
            if (permission.Name == Permissions.SubmitAnyFormForModifyData.Name)
                return Permissions.SubmitAnyFormForModifyOwnData;
            if (permission.Name == Permissions.SubmitAnyFormForDeleteData.Name)
                return Permissions.SubmitAnyFormForDeleteOwnData;
            if (permission.Name == Permissions.ShowAnyForm.Name)
                return Permissions.ShowAnyFormWithOwnData;
            return null;
        }
    }
}
