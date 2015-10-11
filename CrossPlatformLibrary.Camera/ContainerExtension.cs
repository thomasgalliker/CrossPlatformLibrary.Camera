using CrossPlatformLibrary.IoC;

namespace CrossPlatformLibrary.Camera
{
    public class ContainerExtension : IContainerExtension
    {
        public void Initialize(ISimpleIoc container)
        {
            //container.RegisterWithConvention<IMedia>();
            container.RegisterWithConvention<IMediaAccess>();
            container.RegisterWithConvention<IMediaLibrary>();
        }
    }
}
