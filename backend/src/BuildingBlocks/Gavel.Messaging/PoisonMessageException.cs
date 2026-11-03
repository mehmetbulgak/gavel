namespace Gavel.Messaging;

public sealed class PoisonMessageException(string message, Exception? innerException = null) : Exception(message, innerException);
