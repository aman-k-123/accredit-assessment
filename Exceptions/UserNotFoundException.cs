using System;

namespace AccreditAssessment.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string username)
            : base($"User with {username} not found")
        {
        }
    }
}