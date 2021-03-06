﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Burrow.Extras;
using Burrow.Tests.Extras.RabbitSetupTests;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

// ReSharper disable InconsistentNaming
namespace Burrow.Tests.Extras.PriorityQueuesRabbitSetupTests
{
    [TestFixture]
    public class MethodSetupExchangeAndQueueFor
    {
        private IRouteFinder _routeFinder = Substitute.For<IRouteFinder>();
        private RouteSetupData _priorityRouteSetupData;

        [SetUp]
        public void Setup()
        {
            _routeFinder.FindExchangeName<Customer>().Returns("Exchange.Customer");
            _routeFinder.FindQueueName<Customer>(null).ReturnsForAnyArgs("Queue.Customer");
            _routeFinder.FindRoutingKey<Customer>().Returns("Customer");

            _priorityRouteSetupData = new RouteSetupData
            {
                RouteFinder = _routeFinder,
                ExchangeSetupData = new HeaderExchangeSetupData(),
                QueueSetupData = new PriorityQueueSetupData(3)
            };
        }

        [Test, ExpectedException(typeof(InvalidExchangeTypeException))]
        public void Should_throw_exception_if_try_to_create_not_header_exchange()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);
            var normalRouteSetupData = new RouteSetupData
            {
                RouteFinder = _routeFinder,
                ExchangeSetupData = new ExchangeSetupData(),
                QueueSetupData = new QueueSetupData()
            };

            // Action
            setup.CreateRoute<Customer>(normalRouteSetupData);
        }


        [Test]
        public void Should_create_normal_queue_if_not_providing_PriorityQueueSetupData()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);
            var normalRouteSetupData = new RouteSetupData
            {
                RouteFinder = _routeFinder,
                ExchangeSetupData = new HeaderExchangeSetupData(),
                QueueSetupData = new QueueSetupData
                {
                    AutoExpire = 10000,
                    MessageTimeToLive = 10000000
                }
            };

            // Action
            setup.CreateRoute<Customer>(normalRouteSetupData);

            // Assert
            model.Received().ExchangeDeclare("Exchange.Customer", "headers", true, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueDeclare("Queue.Customer", true, false, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueBind("Queue.Customer", "Exchange.Customer", "Customer", normalRouteSetupData.OptionalBindingData);
        }

        [Test]
        public void Should_create_Priority_queue_if_provide_PriorityQueueSetupData()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model, Substitute.For<IRabbitWatcher>());
            Func<IDictionary<string, object>,int, bool> eval = (arg, priority) => 
            {
                return "all".Equals(arg["x-match"]) && priority.ToString(CultureInfo.InvariantCulture).Equals(arg["Priority"]);
            };

            // Action
            setup.CreateRoute<Customer>(_priorityRouteSetupData);
            

            // Assert
            model.Received().ExchangeDeclare("Exchange.Customer", "headers", true, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueDeclare("Queue.Customer_Priority0", true, false, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueDeclare("Queue.Customer_Priority1", true, false, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueDeclare("Queue.Customer_Priority2", true, false, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueDeclare("Queue.Customer_Priority3", true, false, false, Arg.Any<IDictionary<string, object>>());
            model.Received().QueueBind("Queue.Customer_Priority0", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 0)));
            model.Received().QueueBind("Queue.Customer_Priority1", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 1)));
            model.Received().QueueBind("Queue.Customer_Priority2", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 2)));
            model.Received().QueueBind("Queue.Customer_Priority3", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 3)));
        }


        [Test]
        public void Should_not_throw_excepton_if_cannot_create_PRIORITY_queues()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            model.When(x => x.QueueDeclare(Arg.Any<string>(), true, false, false, Arg.Any<IDictionary<string, object>>())).Do(callInfo =>
            {
                throw new Exception();
            });
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);
            Func<IDictionary<string, object>, int, bool> eval = (arg, priority) => "all".Equals(arg["x-match"]) && priority.ToString(CultureInfo.InvariantCulture).Equals(arg["Priority"]);

            // Action
            setup.CreateRoute<Customer>(_priorityRouteSetupData);

            // Assert
            model.Received().QueueBind("Queue.Customer_Priority0", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 0)));
            model.Received().QueueBind("Queue.Customer_Priority1", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 1)));
            model.Received().QueueBind("Queue.Customer_Priority2", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 2)));
            model.Received().QueueBind("Queue.Customer_Priority3", "Exchange.Customer", "Customer", Arg.Is<IDictionary<string, object>>(x => eval(x, 3)));
        }

        [Test]
        public void Should_not_throw_excepton_if_cannot_bind_PRIORITY_queues()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            model.When(x => x.QueueBind(Arg.Any<string>(), "Exchange.Customer", "Customer", Arg.Any<IDictionary<string, object>>())).Do(callInfo =>
            {
                throw new Exception();
            });
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);
            // Action
            setup.CreateRoute<Customer>(_priorityRouteSetupData);
        }

        [Test]
        public void Should_catch_OperationInterruptedException_when_trying_to_create_an_exist_queue_but_configuration_not_match()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            model.When(x => x.QueueDeclare(Arg.Any<string>(), true, false, false, Arg.Any<IDictionary<string, object>>())).Do(callInfo =>
            {
                throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 1, "PRECONDITION_FAILED - "));
            });
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);

            // Action
            setup.CreateRoute<Customer>(_priorityRouteSetupData);
        }

        [Test]
        public void Should_catch_OperationInterruptedException_and_log_error_when_trying_to_create_a_queue()
        {
            // Arrange
            var model = Substitute.For<IModel>();
            model.When(x => x.QueueDeclare(Arg.Any<string>(), true, false, false, Arg.Any<IDictionary<string, object>>())).Do(callInfo =>
            {
                throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 1, "Other error"));
            });
            var setup = PriorityQueuesRabbitSetupForTest.CreateRabbitSetup(model);

            // Action
            setup.CreateRoute<Customer>(_priorityRouteSetupData);
        }
    }
}
// ReSharper restore InconsistentNaming