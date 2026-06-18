namespace GestoraWebApi.Auth
{
    public static class Roles
    {
        public const string Admin   = "Admin";
        public const string Staff   = "Staff";
        public const string Cliente = "Cliente";

        // Combinazioni pronte per [Authorize(Roles = ...)]
        public const string AdminOrStaff           = Admin + "," + Staff;
        public const string AdminOrStaffOrCliente  = Admin + "," + Staff + "," + Cliente;
    }
}
