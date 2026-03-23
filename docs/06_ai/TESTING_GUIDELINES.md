# Testing Guidelines

## Overview

CephasOps backend uses xUnit for unit and integration testing. This guide covers testing patterns, best practices, and setup.

## Test Projects

### CephasOps.Application.Tests
- **Purpose:** Unit tests for application services
- **Location:** `backend/tests/CephasOps.Application.Tests`
- **Scope:** Service layer logic, business rules, calculations

### CephasOps.Api.Tests
- **Purpose:** Integration tests for API controllers
- **Location:** `backend/tests/CephasOps.Api.Tests`
- **Scope:** End-to-end API testing, request/response validation

## Testing Framework

### Dependencies
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **Microsoft.AspNetCore.Mvc.Testing** - API testing
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for integration tests

## Unit Testing Patterns

### Service Testing

```csharp
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    [Fact]
    public async Task CreateOrderAsync_WhenValid_ShouldCreateOrder()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.CompanyId).Returns(companyId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        // Act
        var result = await _orderService.CreateOrderAsync(...);

        // Assert
        result.Should().NotBeNull();
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Once);
    }
}
```

### Mocking Guidelines

1. **Mock External Dependencies:** Repositories, external services, HTTP clients
2. **Don't Mock Domain Logic:** Test actual business logic
3. **Verify Interactions:** Use `Verify()` to ensure methods are called correctly
4. **Setup Sequences:** Use `SetupSequence()` for multiple return values

## Integration Testing Patterns

### API Testing

```csharp
public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders_WhenAuthenticated_ShouldReturnOrders()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Test Coverage

### Priority Areas

1. **Business Logic:** All service methods
2. **Calculations:** Payroll, P&L, billing calculations
3. **Validation:** Input validation, business rules
4. **Error Handling:** Exception scenarios
5. **API Endpoints:** All controller actions

### Coverage Goals

- **Unit Tests:** 80%+ coverage for services
- **Integration Tests:** All critical API endpoints
- **Edge Cases:** Boundary conditions, error scenarios

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/CephasOps.Application.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio

- Use Test Explorer to run individual tests
- Set breakpoints in test methods for debugging
- View test output and coverage reports

## Best Practices

### 1. Test Naming
- Use descriptive names: `MethodName_WhenCondition_ShouldExpectedResult`
- Example: `CalculatePayroll_WhenEmployeeHasOvertime_ShouldIncludeOvertimePay`

### 2. Arrange-Act-Assert (AAA)
- **Arrange:** Set up test data and mocks
- **Act:** Execute the method under test
- **Assert:** Verify the results

### 3. One Assertion Per Test
- Focus each test on a single behavior
- Makes failures easier to diagnose

### 4. Test Data
- Use realistic test data
- Consider edge cases (null, empty, boundary values)
- Use builders or factories for complex objects

### 5. Isolation
- Each test should be independent
- Don't rely on test execution order
- Clean up after tests (if needed)

## Common Test Scenarios

### Service Tests
- ✅ Valid input → Success
- ✅ Invalid input → Validation error
- ✅ Not found → Returns null/throws
- ✅ Unauthorized → Throws exception
- ✅ Business rule violations → Throws exception

### API Tests
- ✅ Authenticated request → Success
- ✅ Unauthenticated request → 401 Unauthorized
- ✅ Invalid request → 400 Bad Request
- ✅ Not found → 404 Not Found
- ✅ Server error → 500 Internal Server Error

## Continuous Integration

Tests should run automatically in CI/CD:

1. **On Commit:** Run unit tests
2. **On PR:** Run all tests + coverage
3. **On Merge:** Run full test suite

## Troubleshooting

### Tests Failing Intermittently
- Check for shared state between tests
- Verify async/await usage
- Check for race conditions

### Mock Not Working
- Verify mock setup matches actual method signature
- Check that mock is injected correctly
- Use `Verify()` to see if method was called

### Integration Test Failures
- Verify test database is set up correctly
- Check authentication tokens are valid
- Verify API routes match expectations

