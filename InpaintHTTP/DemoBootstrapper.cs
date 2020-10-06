namespace InpaintHTTP
{
    using Nancy;
    using Nancy.Conventions;
    using Nancy.TinyIoc;

    public class DemoBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IAppConfiguration appConfig;

        public DemoBootstrapper()
        {
        }

        public DemoBootstrapper(IAppConfiguration appConfig)
        {
            this.appConfig = appConfig;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<IAppConfiguration>(appConfig);
        }

        // used to let Nancy know which path we can use
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.Clear(); // remove defaults, if any
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("css", "/TestWebsite/css"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("js", "/TestWebsite/js"));
        }
    }
}