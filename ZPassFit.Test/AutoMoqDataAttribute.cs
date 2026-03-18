using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using AutoFixture.Xunit3;
using Moq;

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