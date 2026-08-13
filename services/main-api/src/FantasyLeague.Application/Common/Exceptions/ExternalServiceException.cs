namespace FantasyLeague.Application.Common.Exceptions;

public class ExternalServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
