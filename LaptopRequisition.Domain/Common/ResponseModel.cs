using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Domain.Common;

public class Response
{
    public bool Success { get; }
    public ResponseCode Code { get; }
    public string Message { get; }
    public List<string> Errors { get; }

    protected Response(bool success, ResponseCode code, List<string> errors = null)
    {
        Success = success;
        Code = code;
        Message = ErrorMessageProvider.Get(code);
        Errors = errors ?? new List<string>();
    }

    public bool HasErrors => Errors.Count > 0;

    public static Response Ok()
        => new(true, ResponseCode.Success);

    public static Response Fail(ResponseCode code, List<string> errors = null)
        => new(false, code, errors);
}


// public class Response<T> : Response
// {
//     public T Data { get; }
//
//     private Response(bool success, ResponseCode code, T data, List<string> errors = null)
//         : base(success, code, errors)
//     {
//         Data = data;
//     }
//
//     public static Response<T> Ok(T data)
//         => new(true, ResponseCode.Success, data);
//     
// }

public class Response<T> : Response
{
    public T Data { get; }

    private Response(bool success, ResponseCode code, T data, List<string> errors = null)
        : base(success, code, errors)
    {
        Data = data;
    }

    public static Response<T> Ok(T data)
        => new(true, ResponseCode.Success, data);

    public new static Response<T> Fail(ResponseCode code, List<string> errors = null)
        => new(false, code, default, errors);
}