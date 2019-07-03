using System.ComponentModel;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    public enum ErrorCodes
    {
        [Description("A generic error.")] Generic = 0,

        [Description("Required parameter is missing.")]
        RequiredParameterMissing = 10,

        [Description("Incompatible Subsonic REST protocol version. Client must upgrade.")]
        IncompatibleClientRestProtocolVersion = 20,

        [Description("Incompatible Subsonic REST protocol version. Server must upgrade.")]
        IncompatibleServerRestProtocolVersion = 30,

        [Description("Wrong username or password.")]
        WrongUsernameOrPassword = 40,

        [Description("Token authentication not supported for LDAP users.")]
        TokenAuthenticatinNotSupportedForLDAP = 41,

        [Description("User is not authorized for the given operation.")]
        UserIsNotAuthorizedForGivenOperation = 50,

        [Description(
            "The trial period for the Subsonic server is over. Please upgrade to Subsonic Premium. Visit subsonic.org for details.")]
        TrialPeriodSubsonicServerHasExpired = 60,

        [Description("The requested data was not found")]
        TheRequestedDataWasNotFound = 70
    }
}