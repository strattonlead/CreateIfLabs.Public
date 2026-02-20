using CreateIfLabs.AutoMapper.Tests.Models;
using AutoMapper;

namespace CreateIfLabs.AutoMapper.Tests.Actions
{
    /// <summary>
    /// Simple dependency that can be injected into the mapping action.
    /// </summary>
    public class AfterMapLogger
    {
        public bool WasCalled { get; set; }
        public string LastMessage { get; set; }

        public void Log(string message)
        {
            WasCalled = true;
            LastMessage = message;
        }
    }

    /// <summary>
    /// A mapping action that combines two fields – implements our IMappingAction.
    /// Also has a dependency on AfterMapLogger for DI verification.
    /// </summary>
    public class CombineFieldsAction : IMappingAction<AfterMapSource, AfterMapDest>
    {
        private readonly AfterMapLogger _logger;

        public CombineFieldsAction(AfterMapLogger logger)
        {
            _logger = logger;
        }

        public void Process(AfterMapSource source, AfterMapDest destination, ResolutionContext context)
        {
            destination.Combined = source.First + " | " + source.Second;
            _logger.Log("CombineFieldsAction executed");
        }
    }
}
