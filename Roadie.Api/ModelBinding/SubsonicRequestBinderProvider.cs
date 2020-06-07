using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Roadie.Api.ModelBinding
{
    public class SubsonicRequestBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(SubsonicRequest))
            {
                return new SubsonicRequestBinder();
            }

            return null;
        }
    }
}