﻿// MSIX Hero
// Copyright (C) 2024 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Otor.MsixHero.Infrastructure.Cryptography
{
    public class Crypto
    {
        public string Protect(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return plainText;
            }

            var data = System.Text.Encoding.UTF8.GetBytes(plainText);
            var protectedData = ProtectedData.Protect(data, new byte[] { 0x61, 0x73, 0x64, 0x61, 0x6a, 0xc4, 0x85, 0xc5, 0x82, 0x7a, 0x64, 0x61, 0x73, 0x64, 0x40, 0x23, 0x61, 0x64, 0x25, 0x25, 0x25, 0x40, 0x24, 0x23, 0x21, 0x40, 0x34, 0x31, 0x31, 0x24, 0x25, 0x24, 0x23, 0x5e, 0x23, 0x73, 0x64, 0x61, 0x63, 0x62, 0x76, 0x76, 0x61, 0x64, 0x73, 0x61, 0x64, 0x61, 0x72, 0x77, 0x61 }, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedData);
        }

        public string Protect(SecureString plainText)
        {
            if (plainText == null || plainText.Length == 0)
            {
                return string.Empty;
            }

            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(plainText);
                // ReSharper disable once AssignNullToNotNullAttribute
                var data = System.Text.Encoding.UTF8.GetBytes(Marshal.PtrToStringUni(unmanagedString));
                var protectedData = ProtectedData.Protect(data, new byte[] { 0x61, 0x73, 0x64, 0x61, 0x6a, 0xc4, 0x85, 0xc5, 0x82, 0x7a, 0x64, 0x61, 0x73, 0x64, 0x40, 0x23, 0x61, 0x64, 0x25, 0x25, 0x25, 0x40, 0x24, 0x23, 0x21, 0x40, 0x34, 0x31, 0x31, 0x24, 0x25, 0x24, 0x23, 0x5e, 0x23, 0x73, 0x64, 0x61, 0x63, 0x62, 0x76, 0x76, 0x61, 0x64, 0x73, 0x61, 0x64, 0x61, 0x72, 0x77, 0x61 }, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedData);
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
        }

        public string UnprotectUnsafe(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return null;
            }

            var protectedData = Convert.FromBase64String(encryptedText);
            var data = ProtectedData.Unprotect(protectedData, new byte[] { 0x61, 0x73, 0x64, 0x61, 0x6a, 0xc4, 0x85, 0xc5, 0x82, 0x7a, 0x64, 0x61, 0x73, 0x64, 0x40, 0x23, 0x61, 0x64, 0x25, 0x25, 0x25, 0x40, 0x24, 0x23, 0x21, 0x40, 0x34, 0x31, 0x31, 0x24, 0x25, 0x24, 0x23, 0x5e, 0x23, 0x73, 0x64, 0x61, 0x63, 0x62, 0x76, 0x76, 0x61, 0x64, 0x73, 0x61, 0x64, 0x61, 0x72, 0x77, 0x61 }, DataProtectionScope.CurrentUser);

            return System.Text.Encoding.UTF8.GetString(data);
        }

        public string UnprotectUnsafe(SecureString encryptedText)
        {
            return UnprotectUnsafe(FromSecureString(encryptedText));
        }

        public SecureString Unprotect(string encryptedText)
        {
            var unprotected = UnprotectUnsafe(encryptedText);

            if (unprotected == null)
            {
                return new SecureString();
            }

            var sec = new SecureString();
            foreach (var c in unprotected)
            {
                sec.AppendChar(c);
            }

            return sec;
        }
        
        [Obsolete("This method should not be used. It is only for backward compatibility.")]
        public string DecryptString(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            if (string.IsNullOrEmpty(sharedSecret))
            {
                throw new ArgumentNullException(nameof(sharedSecret));
            }

#pragma warning disable SYSLIB0022
            RijndaelManaged aesAlg = null;
#pragma warning restore SYSLIB0022
            
            try
            {
                var baseString = "MSIX-Hero:" + Environment.UserDomainName + "\\" + Environment.UserName;

                using var md5 = MD5.Create();
                var salt = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(baseString.ToLowerInvariant()));

                using var key = new Rfc2898DeriveBytes(sharedSecret, salt);
                var bytes = Convert.FromBase64String(cipherText);

                using var msDecrypt = new MemoryStream(bytes);
#pragma warning disable SYSLIB0022
                aesAlg = new RijndaelManaged();
#pragma warning restore SYSLIB0022
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = ReadByteArray(msDecrypt);
                
                // ReSharper disable once IdentifierTypo
                using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                return srDecrypt.ReadToEnd();
            }
            finally
            {
                if (aesAlg != null)
                {
                    aesAlg.Clear();
                    aesAlg.Dispose();
                }
            }
        }

        private static byte[] ReadByteArray(Stream s)
        {
            var rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            var buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }

            return buffer;
        }

        private static string FromSecureString(SecureString secureString)
        {
            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                // ReSharper disable once AssignNullToNotNullAttribute
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
        }
    }
}
