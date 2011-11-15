namespace NancyFacebookCanvasSample
{
    using Nancy;

    public partial class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureRequestContainer(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            ConfigureFacebookRequestContainer(container, context);
        }

        protected override void RequestStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            pipelines.BeforeRequest.AddItemToStartOfPipeline(c => FacebookRequestStartup(container, pipelines, context));
        }
    }
}