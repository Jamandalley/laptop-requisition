using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Domain.Common;

public class Response
{
    public bool IsSuccessful { get; }
    public ResponseCode Code { get; } // FIX: Changed to ResponseCode
    public string Message { get; }
    public List<string> Errors { get; }

    protected Response(bool isSuccessful, ResponseCode code, List<string> errors = null) // FIX: Changed to ResponseCode
    {
        IsSuccessful = isSuccessful;
        Code = code;
        Message = ErrorMessageProvider.Get(code);
        Errors = errors ?? new List<string>();
    }

    public bool HasErrors => Errors.Count > 0;

    public static Response Ok()
        => new(true, ResponseCode.Success); // FIX: Changed to ResponseCode.Success

    public static Response Fail(ResponseCode code, List<string> errors = null) // FIX: Changed to ResponseCode
        => new(false, code, errors);
}

public class Response<T> : Response
{
    public T Data { get; }

    private Response(bool isSuccessful, ResponseCode code, T data, List<string> errors = null) // FIX: Changed to ResponseCode
        : base(isSuccessful, code, errors)
    {
        Data = data;
    }

    public static Response<T> Ok(T data)
        => new(true, ResponseCode.Success, data); // FIX: Changed to ResponseCode.Success

    public new static Response<T> Fail(ResponseCode code, List<string> errors = null) // FIX: Changed to ResponseCode
        => new(false, code, default, errors);
}