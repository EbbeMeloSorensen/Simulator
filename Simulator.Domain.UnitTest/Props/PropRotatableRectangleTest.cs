using Craft.Math;
using FluentAssertions;
using Simulator.Domain.Props;
using Xunit;

namespace Simulator.Domain.UnitTest.Props;

public class PropRotatableRectangleTest
{
    [Fact]
    public void DistanceToPointTest_1()
    {
        // Arrange
        var propRectangle = new PropRotatableRectangle(1, 3, 2, new Vector2D(2.5, 2), 0);
        var point1 = new Vector2D(3, 4);
        var point2 = new Vector2D(6, 2);
        var point3 = new Vector2D(-2, 2);
        var point4 = new Vector2D(2, -3);
        var point5 = new Vector2D(7, 7);
        var point6 = new Vector2D(12, -5);
        var point7 = new Vector2D(-8, -11);
        var point8 = new Vector2D(-11, 19);

        // Act
        var distance1 = propRectangle.DistanceToPoint(point1);
        var distance2 = propRectangle.DistanceToPoint(point2);
        var distance3 = propRectangle.DistanceToPoint(point3);
        var distance4 = propRectangle.DistanceToPoint(point4);
        var distance5 = propRectangle.DistanceToPoint(point5);
        var distance6 = propRectangle.DistanceToPoint(point6);
        var distance7 = propRectangle.DistanceToPoint(point7);
        var distance8 = propRectangle.DistanceToPoint(point8);

        // Assert
        distance1.Should().BeApproximately(1, 0000.1);
        distance2.Should().BeApproximately(2, 0000.1);
        distance3.Should().BeApproximately(3, 0000.1);
        distance4.Should().BeApproximately(4, 0000.1);
        distance5.Should().BeApproximately(5, 0000.1);
        distance6.Should().BeApproximately(10, 0000.1);
        distance7.Should().BeApproximately(15, 0000.1);
        distance8.Should().BeApproximately(20, 0000.1);
    }

    [Fact]
    public void DistanceToPointTest_2()
    {
        // Arrange
        var propRectangle = new PropRotatableRectangle(1, 3, 2, new Vector2D(2.5, 2), Math.PI / 2);
        var point1 = new Vector2D(3, 4);
        var point2 = new Vector2D(6, 2);
        var point3 = new Vector2D(-2, 2);
        var point4 = new Vector2D(2, -3);
        var point5 = new Vector2D(6.5, 7.5);
        var point6 = new Vector2D(11.5, -5.5);
        var point7 = new Vector2D(-7.5, -11.5);
        var point8 = new Vector2D(-10.5, 19.5);

        // Act
        var distance1 = propRectangle.DistanceToPoint(point1);
        var distance2 = propRectangle.DistanceToPoint(point2);
        var distance3 = propRectangle.DistanceToPoint(point3);
        var distance4 = propRectangle.DistanceToPoint(point4);
        var distance5 = propRectangle.DistanceToPoint(point5);
        var distance6 = propRectangle.DistanceToPoint(point6);
        var distance7 = propRectangle.DistanceToPoint(point7);
        var distance8 = propRectangle.DistanceToPoint(point8);

        // Assert
        distance1.Should().BeApproximately(0.5, 0000.1);
        distance2.Should().BeApproximately(2.5, 0000.1);
        distance3.Should().BeApproximately(3.5, 0000.1);
        distance4.Should().BeApproximately(3.5, 0000.1);
        distance5.Should().BeApproximately(5, 0000.1);
        distance6.Should().BeApproximately(10, 0000.1);
        distance7.Should().BeApproximately(15, 0000.1);
        distance8.Should().BeApproximately(20, 0000.1);
    }
}