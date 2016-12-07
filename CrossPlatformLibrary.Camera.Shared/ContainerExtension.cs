using CrossPlatformLibrary.IoC;

namespace CrossPlatformLibrary.Camera
{
    public class ContainerExtension : IContainerExtension
    {
        public void Initialize(ISimpleIoc container)
        {
            //container.Register<IMedia, Media>();
            container.Register<IMediaAccess, MediaAccess>();
            container.Register<IMediaLibrary, MediaLibrary>();
        }
    }
}
