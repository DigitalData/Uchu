using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Uchu.Core;
using Uchu.Core.Client;

namespace Uchu.World.Behaviors
{
    public abstract class BehaviorBase
    {
        public static readonly List<BehaviorBase> Cache = new List<BehaviorBase>();
        
        private static int EffectIndex { get; set; }
        
        public int BehaviorId { get; set; }

        public abstract BehaviorTemplateId Id { get; }

        public abstract Task BuildAsync();

        private int EffectId { get; set; }

        public virtual async Task ExecuteAsync(ExecutionContext context, ExecutionBranchContext branchContext)
        {
            if (EffectId == 0)
            {
                EffectId = await GetParameter<int>("effectID");

                if (EffectId == default) EffectId = -1;
            }

            if (EffectId == -1) return;

            var effectName = EffectIndex++.ToString();

            context.Associate.Zone.ExcludingMessage(new PlayFXEffectMessage
            {
                Associate = branchContext.Target,
                Secondary = context.Associate,
                EffectId = EffectId,
                Name = effectName
            }, context.Associate as Player);

            var _ = Task.Run(async () =>
            {
                await Task.Delay(branchContext.Duration > 0 ? branchContext.Duration : 1000);
                
                context.Associate.Zone.ExcludingMessage(new StopFXEffectMessage()
                {
                    Associate = branchContext.Target,
                    Name = effectName
                }, context.Associate as Player);
            });
        }

        public virtual Task SyncAsync(ExecutionContext context, ExecutionBranchContext branchContext)
        {
            return Task.CompletedTask;
        }

        public virtual Task DismantleAsync(ExecutionContext context, ExecutionBranchContext branchContext)
        {
            return Task.CompletedTask;
        }

        public static async Task<BehaviorBase> BuildBranch(int behaviorId)
        {
            var cachedBehavior = Cache.FirstOrDefault(c => c.BehaviorId == behaviorId);

            if (cachedBehavior != default) return cachedBehavior;
            
            await using var ctx = new CdClientContext();

            var behavior = await ctx.BehaviorTemplateTable.FirstOrDefaultAsync(
                t => t.BehaviorID == behaviorId
            );
            
            if (behavior?.TemplateID == null) return new EmptyBehavior();
            
            var behaviorTypeId = (BehaviorTemplateId) behavior.TemplateID;
            
            if (!BehaviorTree.Behaviors.TryGetValue(behaviorTypeId, out var behaviorType))
            {
                Logger.Error($"No behavior type of \"{behaviorTypeId}\" found.");
                
                return new EmptyBehavior();
            }

            var instance = (BehaviorBase) Activator.CreateInstance(behaviorType);
            
            instance.BehaviorId = behaviorId;
            
            Cache.Add(instance);

            await instance.BuildAsync();

            return instance;
        }

        protected void RegisterHandle(uint handle, ExecutionContext context, ExecutionBranchContext branchContext)
        {
            context.BehaviorHandles[handle] = async (reader, writer) =>
            {
                var newBranchContext = new ExecutionBranchContext(branchContext.Target)
                {
                    Duration = branchContext.Duration
                };
                
                context.Reader = reader;
                context.Writer = writer;
                
                await SyncAsync(context, newBranchContext);
            };
        }

        protected async Task<BehaviorParameter> GetParameter(string name)
        {
            await using var cdClient = new CdClientContext();
            return await cdClient.BehaviorParameterTable.FirstOrDefaultAsync(p =>
                p.BehaviorID == BehaviorId && p.ParameterID == name
            );
        }

        protected async Task<T> GetParameter<T>(string name) where T : struct
        {
            var param = await GetParameter(name);

            if (param == default) return default;
            
            return param.Value.HasValue ? (T) Convert.ChangeType(param.Value.Value, typeof(T)) : default;
        }

        protected BehaviorParameter[] GetParameters()
        {
            using var cdClient = new CdClientContext();
            return cdClient.BehaviorParameterTable.Where(p =>
                p.BehaviorID == BehaviorId
            ).ToArray();
        }

        public async Task<BehaviorTemplate> GetTemplate()
        {
            await using var cdClient = new CdClientContext();
            return await cdClient.BehaviorTemplateTable.FirstOrDefaultAsync(p =>
                p.BehaviorID == BehaviorId
            );
        }

        protected async Task<BehaviorBase> GetBehavior(string name)
        {
            var action = await GetParameter(name);

            if (action?.Value == null || action.Value.Value.Equals(0)) return new EmptyBehavior();

            return await BuildBranch((int) action.Value);
        }

        protected async Task<BehaviorBase> GetBehavior(uint id)
        {
            if (id == default) return new EmptyBehavior();
            
            return await BuildBranch((int) id);
        }
    }
}