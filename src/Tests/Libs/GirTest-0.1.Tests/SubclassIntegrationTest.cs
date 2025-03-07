﻿using FluentAssertions;
using GObject;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GirTest.Tests;

[TestClass, TestCategory("BindingTest")]
public class SubclassIntegrationTest : Test
{
    [TestMethod]
    public void ShouldBeDerivedFromGObjectObject()
    {
        typeof(SomeSubClass).Should().BeDerivedFrom<GObject.Object>();
    }

    [TestMethod]
    public void ShouldImplementGObjectInstanceFactory()
    {
        typeof(SomeSubClass).Should().Implement(typeof(GObject.InstanceFactory));
    }

    [TestMethod]
    public void ShouldImplementGObjectGTypeProvider()
    {
        typeof(SomeSubClass).Should().Implement(typeof(GObject.GTypeProvider));
    }

    [TestMethod]
    public void ShouldHaveConstructArgumentConstructor()
    {
        typeof(SomeSubClass).Should().HaveConstructor([typeof(GObject.ConstructArgument[])]);
    }

    [TestMethod]
    public void ShoudHaveAGtype()
    {
        SomeSubClass.GetGType().Value.Should().NotBe(0);
    }

    [TestMethod]
    public void GenericSubclassesShouldBePossible()
    {
        var type1 = SomeGenericSubclass<int>.GetGType();
        var type2 = SomeGenericSubclass<int, int>.GetGType();
        var type3 = SomeGenericSubclass<SomeSubClass, string>.GetGType();
        var type4 = SomeGenericSubclass<SomeGenericSubclass<string>>.GetGType();
        var type5 = SomeSubSubClass.GetGType();
        var type6 = SomeContainingClass.SomeNestedGenericSubSubSubClass.GetGType();

        type1.Should().NotBe(type2);
        type1.Should().NotBe(type3);
        type1.Should().NotBe(type4);
        type1.Should().NotBe(type5);
        type1.Should().NotBe(type6);

        type2.Should().NotBe(type3);
        type2.Should().NotBe(type4);
        type2.Should().NotBe(type5);
        type2.Should().NotBe(type6);

        type3.Should().NotBe(type4);
        type3.Should().NotBe(type5);
        type3.Should().NotBe(type6);

        type4.Should().NotBe(type5);
        type4.Should().NotBe(type6);

        type5.Should().NotBe(type6);
    }
}

[Subclass<GObject.Object>]
internal partial class SomeSubClass;

[Subclass<GObject.Object>]
internal partial class SomeGenericSubclass<T>;

[Subclass<GObject.Object>]
internal partial class SomeGenericSubclass<T1, T2>;

[Subclass<SomeSubClass>]
internal partial class SomeSubSubClass;

internal partial class SomeContainingClass
{
    [Subclass<SomeSubSubClass>]
    internal partial class SomeNestedGenericSubSubSubClass;
}
