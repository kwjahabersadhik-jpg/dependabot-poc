namespace CreditCardsSystem.Domain.Common;

public class ApiException : Exception
{
    public List<ValidationError>? Errors { get; set; }
    public string MethodName { get; set; }
    public override string Message { get; }
    public bool InsertSeriLog { get; }
    public bool Continue { get; set; }

    public ApiException(List<ValidationError>? errors = default, string methodName = "", string message = "", bool insertSeriLog = false, bool returnBack = false)
    {
        Errors = errors;
        MethodName = methodName;
        Message = message;
        InsertSeriLog = insertSeriLog;
        Continue = returnBack;
    }
}

public class IntegrationException : Exception
{

    public string CallerMethodName { get; set; }
    public string EndpointUrl { get; set; }
    public string EndpointMethod { get; set; }
    public string Request { get; set; }
    public string Response { get; set; }
    public override string Message { get; }

    public bool InsertSeriLog { get; }
    public bool Continue { get; }
}
