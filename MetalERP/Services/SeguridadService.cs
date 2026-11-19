using System.Security.Cryptography;
using System.Text;

namespace MetalERP.Services
{
    public static class SeguridadService
    {
        public static byte[] GenerarHashSha512(string password, Guid salt)
        {
            // Combina la contraseña con el Salt en formato estándar de SQL Server
            string textoConSalt = password + salt.ToString().ToUpper();
            byte[] bytes = Encoding.UTF8.GetBytes(textoConSalt);

            // Devuelve exactamente 64 bytes (varbinary(64))
            return SHA512.HashData(bytes);
        }

        public static bool VerificarPassword(string passwordIngresado, byte[] hashGuardado, Guid? salt)
        {
            if (salt == null || hashGuardado == null || hashGuardado.Length != 64)
                return false;

            byte[] hashCalculado = GenerarHashSha512(passwordIngresado, salt.Value);

            // Comparación segura en tiempo constante contra ataques de temporización
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashGuardado);
        }
    }
}
