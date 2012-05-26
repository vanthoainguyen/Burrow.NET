﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

// ReSharper disable InconsistentNaming
namespace Burrow.Tests.BurrowConsumerTests
{
    [TestClass]
    public class Constructor
    {
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Should_throw_exception_if_provide_null_channel()
        {
            // Action
            new BurrowConsumer(null, NSubstitute.Substitute.For<IMessageHandler>(),
                               NSubstitute.Substitute.For<IRabbitWatcher>(), "consumerTag", false, 3);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Should_throw_exception_if_provide_null_messageHandler()
        {
            // Action
            new BurrowConsumer(NSubstitute.Substitute.For<IModel>(), null,
                               NSubstitute.Substitute.For<IRabbitWatcher>(), "consumerTag", false, 3);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Should_throw_exception_if_provide_null_watcher()
        {
            // Action
            new BurrowConsumer(NSubstitute.Substitute.For<IModel>(), NSubstitute.Substitute.For<IMessageHandler>(),
                               null, "consumerTag", false, 3);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Should_throw_exception_if_provide_batchSize_less_than_1()
        {
            // Action
            new BurrowConsumer(NSubstitute.Substitute.For<IModel>(), NSubstitute.Substitute.For<IMessageHandler>(),
                               NSubstitute.Substitute.For<IRabbitWatcher>(), "consumerTag", false, 0);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Should_throw_exception_if_provide_null_consumer_tag()
        {
            // Action
            new BurrowConsumer(NSubstitute.Substitute.For<IModel>(), NSubstitute.Substitute.For<IMessageHandler>(),
                               NSubstitute.Substitute.For<IRabbitWatcher>(), null, false, 3);
        }
    }
}
// ReSharper restore InconsistentNaming