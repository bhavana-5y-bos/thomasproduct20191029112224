/* --------------------------- LOGOUT USER -----------------------------------*/
function InitiateLogout() {
    DisplayConfirmationDialog({
        Message: "Are you sure you want to logout?",
        CallFrom: "Logout",
        OkData: { Label: "Yes", Data: null },
        CancelData: { Label: "No", Data: null }
    });
}

function TriggerLogout() {
    $.ajax({
        type: "POST",
        url: "/Auth/Logout",
        success: function (response) {
            location.reload();
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });
}
/* --------------------------- END LOGOUT USER -----------------------------------*/

/* --------------------------- UPDATE PROFILE -----------------------------------*/
function UpdateUsername() {
    $.ajax({
        type: "POST",
        url: "/Profile/UpdateUsername",
        data: JSON.stringify($("#email")[0].value),
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            DisplayDialog({ Success: true, Message: response });
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });
}
/* --------------------------- UPDATE PROFILE -----------------------------------*/

/* --------------------------- DELETE USER -----------------------------------*/
function OnUserDeleteClicked(selectedrow) {
    var selectedUserId = selectedrow.id;
    DisplayConfirmationDialog({
        Message: "Are you sure you want to delete the selected user?",
        CallFrom: "DeleteUser",
        OkData: { Label: "Yes", Data: selectedUserId },
        CancelData: { Label: "No", Data: null }
    });
}

function TriggerDeleteUser(selectedUserId) {
    $.ajax({
        type: "POST",
        url: "/Users/DeleteUser",
        data: JSON.stringify(selectedUserId),
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            DisplayDialog({ Success: true, Message: response });
            $("#tableUsers tr[id='" + selectedUserId + "']").remove();
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });
}
/* --------------------------- END DELETE USER -----------------------------------*/

/* --------------------------- CHANGE PASSWORD BY ADMIN -----------------------------------*/
function OnChangePasswordClicked(selectedrow) {
    var selectedUserId = selectedrow.id;
    $('#hiddenUserIdChangePwd').val(selectedUserId);
    $('#myModal').modal('show');
}

function SetPassword() {
    if ($('#password').val() === $('#confirmpassword').val()) {
        var userId = $("#hiddenUserIdChangePwd").val();

        var data = {};
        data.userId = userId;
        data.password = $('#password').val();

        $.ajax({
            type: "POST",
            url: "/Auth/ForcePasswordChange",
            data: JSON.stringify(data),
            contentType: 'application/json; charset=utf-8',
            success: function (response) {
                $('#hiddenUserIdChangePwd').val();
                DisplayDialog({ Success: true, Message: response });
                $('#myModal').modal('hide');
            },
            failure: function (response) {
                console.log(response.Message);
            },
            error: function (response) {
                console.log(response.Message);
            }
        });
    } else
        $('#message').html('Not Matching').css('color', 'red');
}
/* --------------------------- END CHANGE PASSWORD BY ADMIN -----------------------------------*/

/* --------------------------- EDIT USER -----------------------------------*/
function OnUserEditClicked(selectedrow) {
    var selectedUserId = selectedrow.id;
    selectedUserId = selectedUserId.replace(/\+/g, '%2B');
    $('#hiddenUserEdit').attr("href", "/Users/EditUser?userId=" + selectedUserId);
    $('#hiddenUserEdit')[0].click();
}

function UpdateUserInfo() {

    var userId = $("#hiddenEditUserId")[0].value;
    var emailId = $("#email")[0].value;

    var user = {
        "UpdatedId": userId,
        "FirstName": $("#editUserFirstName")[0].value,
        "LastName": $("#editUserLastName")[0].value,
        "Username": emailId,
        "Email": emailId
    };

    var jsonObject = JSON.stringify(user);
    $.ajax({
        type: "POST",
        url: "/Users/UpdateUserInfo",
        data: jsonObject,
        contentType: 'application/json; charset=utf-8',
        dataType: "text",
        success: function (response) {
            if (response.indexOf('success') > -1) {
                DisplayDialog({ Success: true, Message: response });
            }
            else {
                DisplayDialog({ Success: false, Message: response });
            }
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });

    var isUserAllowed = $('#IsUserAllowed').val();
    if (isUserAllowed === "True") {
        var roleElements = $("#divRoleBase input:checked");
        var rolesList = [];
        if (roleElements !== null && roleElements.length > 0) {
            for (var i = 0; i < roleElements.length; i++) {
                var role = new Object();
                role.Id = roleElements[i].id;
                role.Name = roleElements[i].name;
                rolesList.push(role);
            }
        }

        var data = new Object();
        data.UpdatedRoles = rolesList;
        data.UserId = userId;

        jsonObject = null;
        jsonObject = JSON.stringify(data);

        $.ajax({
            type: "POST",
            url: "/Roles/UpdateUserRolesByAdmin",
            data: jsonObject,
            contentType: 'application/json; charset=utf-8',
            dataType: "text",
            success: function (response) {
                DisplayDialog({ Success: true, Message: response });
            },
            failure: function (response) {
                console.log(response.Message);
            },
            error: function (response) {
                console.log(response.Message);
            }
        });
    }
}
/* --------------------------- END EDIT USER -----------------------------------*/

/* --------------------------- EDIT ROLE -----------------------------------*/
function OnRoleEditClicked(selectedrow) {
    var selectedRoleId = selectedrow.id;
    $('#hiddenRoleEdit').attr("href", "/Roles/EditRole?roleId=" + selectedRoleId);
    $('#hiddenRoleEdit')[0].click();
}
/* --------------------------- END EDIT ROLE -----------------------------------*/

/* --------------------------- DELETE USER -----------------------------------*/
function OnRoleDeleteClicked(selectedrow) {
    var selectedRoleId = selectedrow.id;
    DisplayConfirmationDialog({
        Message: "Are you sure you want to delete the selected role? \n Note: If this role is already associated with users, they no longer will have access to the previliges of this role.",
        CallFrom: "DeleteRole",
        OkData: { Label: "Yes", Data: selectedRoleId },
        CancelData: { Label: "No", Data: null }
    });
}

function TriggerDeleteRole(selectedRoleId) {
    $.ajax({
        type: "POST",
        url: "/Roles/DeleteRole",
        data: JSON.stringify(selectedRoleId),
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            DisplayDialog({ Success: true, Message: response });
            $("#tableRoles tr[id='" + selectedRoleId + "']").remove();
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });
}
/* --------------------------- END DELETE USER -----------------------------------*/

/* --------------------------- ROLE - MANAGE PERMISSIONS -----------------------------------*/
function OnManagePermissionsClicked(selectedrow) {
    var roleId = selectedrow.id;
    var roleName = $('#' + roleId)[0].children[0].innerHTML;
    $('#hiddenRoleManagePerms').attr("href", "/Roles/RoleManagePermissions?roleId=" + roleId + '&roleName=' + roleName );
    $('#hiddenRoleManagePerms')[0].click();
}
/* --------------------------- END ROLE - MANAGE PERMISSIONS -----------------------------------*/

function OnPermissionsSave() {
    var modulesList = $('.modulecheck:checkbox:checked');
    var operationsList = $('.operationcheck:checkbox:checked');
    var ownerId = $("#hiddenOwnerId").val();

    var updatedModulesList = [];
    var updatedOperationsList = [];

    for (i = 0; i < modulesList.length; i++) {
        var permissionSet = new Object();
        permissionSet.ModuleId = modulesList[i].id;
        permissionSet.Name = modulesList[i].name;
        permissionSet.Code = modulesList[i].getAttribute('code');
        permissionSet.IsDefault = modulesList[i].getAttribute('isdefault') === 'isdefault' ? 1 : 0;
        updatedModulesList.push(permissionSet);
    }

    for (j = 0; j < operationsList.length; j++) {
        var permissionsOperation = new Object();
        permissionsOperation.ModuleId = operationsList[j].getAttribute('moduleid');
        permissionsOperation.Name = operationsList[j].name;
        permissionsOperation.Code = operationsList[j].getAttribute('code');
        permissionsOperation.IsDefault = operationsList[j].getAttribute('isdefault') === 'isdefault' ? 1: 0;
        permissionsOperation.OperationId = operationsList[j].id;
        updatedOperationsList.push(permissionsOperation);
    }

    var data = new Object();
    data.OwnerId = ownerId;
    data.Modules = updatedModulesList;
    data.Operations = updatedOperationsList;
    
    $.ajax({
        type: "POST",
        url: "/Permissions/UpdatePermissions",
        data: JSON.stringify(data),
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            DisplayDialog({ Success: true, Message: response });
        },
        failure: function (response) {
            console.log(response.Message);
        },
        error: function (response) {
            console.log(response.Message);
        }
    });
}



function someRandomFunction(currentModuleId) {
    var currentModule = $('a[id^="' + currentModuleId + '"]');
    var parentModuleId = '';
    parentModuleId = currentModule[0].getAttribute("parentmoduleid");

    while (parentModuleId !== "") {
        currentModule.parent().next(".sidebar-submenu").slideDown(200);
        currentModule.parent().addClass("active");

        parentModuleId = currentModule.parent().closest('a')[0].getAttribute("parentmoduleid");
        currentModule = $('a[id^="' + parentModuleId + '"]');
    }

    currentModule.next(".sidebar-submenu").slideDown(200);
    currentModule.parent().addClass("active");
}