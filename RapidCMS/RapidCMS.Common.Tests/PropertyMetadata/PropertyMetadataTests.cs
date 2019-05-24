﻿using System;
using System.Linq.Expressions;
using NUnit.Framework;
using RapidCMS.Common.Helpers;
using RapidCMS.Common.Models;

namespace RapidCMS.Common.Tests.PropertyMetadata
{
    public class PropertyMetadataTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void BasicProperty()
        {
            var instance = new BasicClass { Test = "Test Value" };
            Expression<Func<BasicClass, string>> func = (BasicClass x) => x.Test;

            var data = PropertyMetadataHelper.GetPropertyMetadata(func);

            Assert.IsNotNull(data);
            Assert.AreEqual("Test", data.PropertyName);
            Assert.AreEqual("Test Value", data.Getter(instance));
            Assert.AreEqual("Test Value", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);
            Assert.AreEqual(typeof(BasicClass), data.ObjectType);

            data.Setter(instance, "New Value");

            Assert.AreEqual("New Value", instance.Test);
        }

        [Test]
        public void BasicNonStringProperty()
        {
            var instance = new BasicClass { Test = "Test Value", Id = 1 };
            Expression<Func<BasicClass, int>> func = (BasicClass x) => x.Id;

            var data = PropertyMetadataHelper.GetPropertyMetadata(func);

            Assert.IsNotNull(data);
            Assert.AreEqual("Id", data.PropertyName);
            Assert.AreEqual(1, data.Getter(instance));
            Assert.AreEqual(null, data.StringGetter);
            Assert.AreEqual(typeof(int), data.PropertyType);
            Assert.AreEqual(typeof(BasicClass), data.ObjectType);

            data.Setter(instance, 2);

            Assert.AreEqual(2, instance.Id);
        }

        [Test]
        public void NestedProperty()
        {
            var instance = new ParentClass { Basic = new BasicClass { Test = "Test Value" } };
            Expression<Func<ParentClass, string>> func = (ParentClass x) => x.Basic.Test;

            var data = PropertyMetadataHelper.GetPropertyMetadata(func);

            Assert.IsNotNull(data);
            Assert.AreEqual("BasicTest", data.PropertyName);
            Assert.AreEqual("Test Value", data.Getter(instance));
            Assert.AreEqual("Test Value", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);
            Assert.AreEqual(typeof(ParentClass), data.ObjectType);

            data.Setter(instance, "New Value");

            Assert.AreEqual("New Value", instance.Basic.Test);
        }

        [Test]
        public void NestedNonStringProperty()
        {
            var instance = new ParentClass { Basic = new BasicClass { Test = "Test Value", Id = 1 } };
            Expression<Func<ParentClass, int>> func = (ParentClass x) => x.Basic.Id;

            var data = PropertyMetadataHelper.GetPropertyMetadata(func);

            Assert.IsNotNull(data);
            Assert.AreEqual("BasicId", data.PropertyName);
            Assert.AreEqual(1, data.Getter(instance));
            Assert.AreEqual(null, data.StringGetter);
            Assert.AreEqual(typeof(int), data.PropertyType);
            Assert.AreEqual(typeof(ParentClass), data.ObjectType);

            data.Setter(instance, 2);

            Assert.AreEqual(2, instance.Basic.Id);
        }

        [Test]
        public void BasicStringExpression()
        {
            var instance = new BasicClass { Test = "Test Value" };
            Expression<Func<BasicClass, string>> func = (BasicClass x) => $"{x.Test}";

            var data = PropertyMetadataHelper.GetExpressionMetadata(func) as IExpressionMetadata;

            Assert.IsNotNull(data);
            Assert.AreEqual("Test Value", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);

            Assert.Throws(typeof(ArgumentException), () => PropertyMetadataHelper.GetPropertyMetadata(func));
        }

        [Test]
        public void BasicStringExpression2()
        {
            var instance = new BasicClass { Test = "Test Value" };
            Expression<Func<BasicClass, string>> func = (BasicClass x) => $"Blaat";

            var data = PropertyMetadataHelper.GetExpressionMetadata(func) as IExpressionMetadata;

            Assert.IsNotNull(data);
            Assert.AreEqual("Blaat", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);

            Assert.Throws(typeof(ArgumentException), () => PropertyMetadataHelper.GetPropertyMetadata(func));
        }

        [Test]
        public void BasicStringExpression3()
        {
            var instance = new BasicClass { Test = "Test Value" };
            Expression<Func<BasicClass, string>> func = (BasicClass x) => string.Join(' ', x.Test.ToCharArray());

            var data = PropertyMetadataHelper.GetExpressionMetadata(func) as IExpressionMetadata;

            Assert.IsNotNull(data);
            Assert.AreEqual("T e s t   V a l u e", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);

            Assert.Throws(typeof(ArgumentException), () => PropertyMetadataHelper.GetPropertyMetadata(func));
        }

        [Test]
        public void BasicNonStringExpression()
        {
            var instance = new BasicClass { Test = "Test Value", Id = 3 };
            Expression<Func<BasicClass, int>> func = (BasicClass x) => x.Id;

            var data = PropertyMetadataHelper.GetExpressionMetadata(func) as IExpressionMetadata;

            Assert.IsNotNull(data);
            Assert.AreEqual(null, data.StringGetter);
            Assert.AreEqual(typeof(int), data.PropertyType);
        }

        [Test]
        public void NestedStringExpression()
        {
            var instance = new ParentClass { Basic = new BasicClass { Test = "Test Value" } };
            Expression<Func<ParentClass, string>> func = (ParentClass x) => $"{x.Basic} {x.Basic.Test}";

            var data = PropertyMetadataHelper.GetExpressionMetadata(func) as IExpressionMetadata;

            Assert.IsNotNull(data);
            Assert.AreEqual("RapidCMS.Common.Tests.PropertyMetadata.PropertyMetadataTests+BasicClass Test Value", data.StringGetter(instance));
            Assert.AreEqual(typeof(string), data.PropertyType);

            Assert.Throws(typeof(ArgumentException), () => PropertyMetadataHelper.GetPropertyMetadata(func));
        }

        class BasicClass
        {
            public string Test { get; set; }
            public int Id { get; set; }
        }

        class ParentClass
        {
            public BasicClass Basic { get; set; }
        }
    }
}