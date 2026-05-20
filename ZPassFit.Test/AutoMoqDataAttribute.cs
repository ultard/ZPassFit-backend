using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Options.Auth;
using ZPassFit.Options.Payments;

namespace ZPassFit.Test;

public sealed class AutoMoqDataAttribute() : AutoDataAttribute(CreateFixture)
{
    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        fixture.Customize(new AutoMoqCustomization
        {
            ConfigureMembers = true
        });

        fixture.Register(() =>
            Microsoft.Extensions.Options.Options.Create(
                new JwtOptions
                {
                    Secret = "01234567890123456789012345678901",
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    AccessTokenExpirationMinutes = 60,
                    RefreshTokenExpirationDays = 30
                }
            )
        );

        fixture.Register(() => Microsoft.Extensions.Options.Options.Create(new PaymentMethodsOptions()));

        fixture.Customizations.Insert(0, new StrictMockBuilder());

        return fixture;
    }

    private sealed class StrictMockBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is not Type { IsGenericType: true } t || t.GetGenericTypeDefinition() != typeof(Mock<>))
                return new NoSpecimen();

            var mockedType = t.GetGenericArguments()[0];
            var mockType = typeof(Mock<>).MakeGenericType(mockedType);
            return Activator.CreateInstance(mockType, MockBehavior.Strict)!;
        }
    }
}