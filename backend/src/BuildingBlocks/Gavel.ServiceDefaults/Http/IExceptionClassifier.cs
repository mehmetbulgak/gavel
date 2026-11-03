namespace Gavel.ServiceDefaults.Http;

public interface IExceptionClassifier
{
    ExceptionClassification? Classify(Exception exception);
}
