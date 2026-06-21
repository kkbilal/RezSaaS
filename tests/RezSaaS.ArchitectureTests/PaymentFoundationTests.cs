using RezSaaS.Modules.Payments.Domain;

namespace RezSaaS.ArchitectureTests;

public sealed class PaymentFoundationTests
{
    private static readonly DateTimeOffset TestTime =
        new(2026, 6, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PaymentPolicyRejectsHostedCheckoutWithoutProvider()
    {
        PaymentPolicy policy = PaymentPolicy.CreateDisabled(
            Guid.CreateVersion7(),
            branchId: null,
            Guid.CreateVersion7(),
            TestTime);

        Assert.Throws<ArgumentException>(() => policy.Configure(
            PaymentCollectionMode.Deposit,
            fixedAmount: 250,
            percentage: null,
            currencyCode: "TRY",
            providerKey: "",
            hostedCheckoutEnabled: true,
            noShowFixedAmount: null,
            noShowPercentage: null,
            Guid.CreateVersion7(),
            TestTime));
    }

    [Fact]
    public void PaymentIntentRequiresBookingReference()
    {
        Assert.Throws<ArgumentException>(() => PaymentIntent.CreateHostedCheckout(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            appointmentRequestId: null,
            appointmentId: null,
            PaymentIntentPurpose.AppointmentDeposit,
            amount: 250,
            currencyCode: "TRY",
            providerKey: "sandbox-provider",
            TestTime,
            TestTime.AddMinutes(15)));
    }

    [Fact]
    public void PaymentWebhookEventAcceptsOnlyPayloadHash()
    {
        string payloadHash = new('A', 64);

        PaymentWebhookEvent webhookEvent = PaymentWebhookEvent.RecordReceived(
            "sandbox-provider",
            "evt_123",
            "payment.succeeded",
            payloadHash,
            TestTime);

        Assert.Equal(payloadHash, webhookEvent.PayloadSha256);
        Assert.Throws<ArgumentException>(() => PaymentWebhookEvent.RecordReceived(
            "sandbox-provider",
            "evt_124",
            "payment.succeeded",
            "{\"card\":\"4111111111111111\"}",
            TestTime));
    }
}
