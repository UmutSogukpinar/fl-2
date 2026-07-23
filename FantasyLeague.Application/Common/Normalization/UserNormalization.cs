using FantasyLeague.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Normalization
{
    internal class UserNormalization
    {

        public static void NormalizeCreateUserRequest(CreateUserRequest request)
        {
            NormalizeEmail(request.Email);  
            NormalizeUsername(request.Username);
        }

        public static void NormalizeUpdateUserRequest(UpdateUserRequest request)
        {
            NormalizeEmail(request.Email);
            NormalizeUsername(request.Username);
        }

        private static void NormalizeEmail(string? email)
        {
            email = email?.ToLower();
        }

        private static void NormalizeUsername(string? username)
        {
            username = username?.ToLower();
        }
    }
}
