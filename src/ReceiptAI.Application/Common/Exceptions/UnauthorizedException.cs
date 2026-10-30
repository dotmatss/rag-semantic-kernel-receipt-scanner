namespace ReceiptAI.Application.Common.Exceptions;

public class UnauthorizedException(string message = "Unauthorized.") : Exception(message);
