using System.Threading.Tasks;

namespace Uchu.World.Behaviors
{
    public class AirMovementBehavior : BehaviorBase
    {
        public override BehaviorTemplateId Id => BehaviorTemplateId.AirMovement;
        
        public override Task BuildAsync()
        {
            return Task.CompletedTask;
        }

        public override async Task ExecuteAsync(ExecutionContext context, ExecutionBranchContext branchContext)
        {
            await base.ExecuteAsync(context, branchContext);

            var handle = context.Reader.Read<uint>();

            context.Writer.Write(handle);
            
            RegisterHandle(handle, context, branchContext);
        }

        public override async Task SyncAsync(ExecutionContext context, ExecutionBranchContext branchContext)
        {
            await base.ExecuteAsync(context, branchContext);
            
            var actionId = context.Reader.Read<uint>();

            context.Writer.Write(actionId);

            var action = await GetBehavior(actionId);

            var id = context.Reader.Read<ulong>();

            context.Writer.Write(id);

            context.Associate.Zone.TryGetGameObject((long) id, out var target);

            var branch = new ExecutionBranchContext(target)
            {
                Duration = branchContext.Duration
            };

            await action.ExecuteAsync(context, branch);
        }
    }
}