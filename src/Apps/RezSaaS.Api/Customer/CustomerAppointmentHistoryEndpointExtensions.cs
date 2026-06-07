using System.Security.Claims;
using RezSaaS.Api.Session;

namespace RezSaaS.Api.Customer;

public static class CustomerAppointmentHistoryEndpointExtensions
{
    public static IEndpointRouteBuilder MapCustomerAppointmentHistoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder customerAppointments = endpoints
            .MapGroup("/api/customer/appointment-history")
            .WithTags("Customer Appointment History")
            .RequireAuthorization()
            .RequireRateLimiting(SessionRateLimitPolicyNames.Bootstrap);

        customerAppointments
            .MapGet(
                string.Empty,
                async (
                    string? status,
                    int? take,
                    ClaimsPrincipal user,
                    CustomerAppointmentHistoryComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    CustomerAppointmentHistoryResult result =
                        await composer.GetAsync(
                            user,
                            status,
                            take,
                            cancellationToken);

                    if (result.Outcome == CustomerAppointmentHistoryOutcome.Success)
                    {
                        return Results.Ok(new CustomerAppointmentHistoryResponse(result.Items));
                    }

                    CustomerAppointmentHistoryErrorResponse error =
                        new(result.ErrorCode ?? "CUSTOMER_APPOINTMENT_HISTORY_FAILED");

                    return result.Outcome switch
                    {
                        CustomerAppointmentHistoryOutcome.Unauthorized => Results.Unauthorized(),
                        CustomerAppointmentHistoryOutcome.BadRequest => Results.BadRequest(error),
                        _ => Results.UnprocessableEntity(error),
                    };
                })
            .WithName("GetCustomerAppointmentHistory")
            .Produces<CustomerAppointmentHistoryResponse>()
            .Produces<CustomerAppointmentHistoryErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
