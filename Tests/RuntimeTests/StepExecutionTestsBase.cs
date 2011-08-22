using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MiniDi;
using NUnit.Framework;
using Rhino.Mocks;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.Utils;

namespace TechTalk.SpecFlow.RuntimeTests
{
    public class StepExecutionTestsBase
    {
        protected MockRepository MockRepository;
        protected CultureInfo FeatureLanguage;
        protected IStepArgumentTypeConverter StepArgumentTypeConverterStub;

        protected IContextManager ContextManagerStub;

        #region dummy test tracer
        public class DummyTestTracer : ITestTracer
        {
            public void TraceStep(StepArgs stepArgs, bool showAdditionalArguments)
            {
            }

            public void TraceWarning(string text)
            {
                Console.WriteLine("TraceWarning: {0}", text);
            }

            public void TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
            {
            }

            public void TraceStepSkipped()
            {
                Console.WriteLine("TraceStepSkipped");
            }

            public void TraceStepPending(BindingMatch match, object[] arguments)
            {
                Console.WriteLine("TraceStepPending");
            }

            public void TraceBindingError(BindingException ex)
            {
                Console.WriteLine("TraceBindingError: {0}", ex);
            }

            public void TraceError(Exception ex)
            {
                Console.WriteLine("TraceError: {0}", ex);
            }

            public void TraceNoMatchingStepDefinition(StepArgs stepArgs, ProgrammingLanguage targetLanguage, List<BindingMatch> matchesWithoutScopeCheck)
            {
                Console.WriteLine("TraceNoMatchingStepDefinition");
            }

            public void TraceDuration(TimeSpan elapsed, MethodInfo methodInfo, object[] arguments)
            {
                //nop
            }

            public void TraceDuration(TimeSpan elapsed, string text)
            {
                //nop
            }
        }
        #endregion

        protected virtual CultureInfo GetFeatureLanguage()
        {
            return new CultureInfo("en-US");
        }     
        
        protected virtual CultureInfo GetBindingCulture()
        {
            return new CultureInfo("en-US");
        }        

        [SetUp]
        public virtual void SetUp()
        {
            ObjectContainer.Reset();

            MockRepository = new MockRepository();

            // FeatureContext and ScenarioContext is needed, because the [Binding]-instances live there
            FeatureLanguage = GetFeatureLanguage();
            CultureInfo bindingCulture = GetBindingCulture();

            ContextManagerStub = new ContextManager(MockRepository.Stub<ITestTracer>());
            ContextManagerStub.InitializeFeatureContext(new FeatureInfo(FeatureLanguage, "test feature", null), bindingCulture);
            ContextManagerStub.InitializeScenarioContext(new ScenarioInfo("test scenario"), null);

            StepArgumentTypeConverterStub = MockRepository.Stub<IStepArgumentTypeConverter>();
        }

        protected TestRunner GetTestRunnerFor(params Type[] bindingTypes)
        {
            return GetTestRunnerFor(null, bindingTypes);
        }

        protected TestRunner GetTestRunnerFor(Action<IObjectContainer> registerMocks, params Type[] bindingTypes)
        {
            return TestRunner.CreateTestRunnerForCompatibility(
                container =>
                    {
                        container.RegisterTypeAs<DummyTestTracer, ITestTracer>();
                        container.RegisterInstanceAs(ContextManagerStub);

                        var bindingRegistry = (BindingRegistry) container.Resolve<IBindingRegistry>();
                        foreach (var bindingType in bindingTypes)
                            bindingRegistry.BuildBindingsFromType(bindingType);

                        if (registerMocks != null)
                            registerMocks(container);
                    });
        }

        protected TestRunner GetTestRunnerFor<TBinding>(out TBinding bindingInstance)
        {
            return GetTestRunnerFor(null, out bindingInstance);
        }

        protected TestRunner GetTestRunnerFor<TBinding>(Action<IObjectContainer> registerMocks, out TBinding bindingInstance)
        {
            TestRunner testRunner = GetTestRunnerFor(registerMocks, typeof(TBinding));

            bindingInstance = MockRepository.StrictMock<TBinding>();
            testRunner.ScenarioContext.SetBindingInstance(typeof(TBinding), bindingInstance);
            return testRunner;
        }

        protected TestRunner GetTestRunnerWithConverterStub<TBinding>(out TBinding bindingInstance)
        {
            return GetTestRunnerFor(c => c.RegisterInstanceAs(StepArgumentTypeConverterStub), out bindingInstance);
        }

        protected TestStatus GetLastTestStatus()
        {
            return ContextManagerStub.ScenarioContext.TestStatus;
        }
    }
}