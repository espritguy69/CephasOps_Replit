using Microsoft.AspNetCore.Http;
using System.Linq;

namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Reads the active department scope from HTTP headers or query parameters.
/// </summary>
public class DepartmentRequestContext : IDepartmentRequestContext
{
    private const string HeaderName = "X-Department-Id";
    private const string QueryParameterName = "departmentId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private bool _initialized;
    private Guid? _departmentId;

    public DepartmentRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? DepartmentId
    {
        get
        {
            EnsureInitialized();
            return _departmentId;
        }
    }

    public bool HasDepartmentScope => DepartmentId.HasValue;

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            if (Guid.TryParse(headerValues.FirstOrDefault(), out var headerDepartmentId))
            {
                _departmentId = headerDepartmentId;
                return;
            }
        }

        if (httpContext.Request.Query.TryGetValue(QueryParameterName, out var queryValues))
        {
            if (Guid.TryParse(queryValues.FirstOrDefault(), out var queryDepartmentId))
            {
                _departmentId = queryDepartmentId;
            }
        }
    }
}


