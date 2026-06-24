// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Wraps an <see cref="AzureStorageResource" /> in a type that exposes container extension methods.
/// </summary>
/// <param name="innerResource">The inner resource used to store annotations.</param>
public class AzureStorageEmulatorResource(AzureStorageResource innerResource) : ContainerResource(innerResource.Name), IResource
{
    private readonly AzureStorageResource _innerResource = innerResource ?? throw new ArgumentNullException(nameof(innerResource));

    /// <inheritdoc/>
    public override string Name => _innerResource.Name;

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;
}

/// <summary>
/// Wraps an <see cref="AzureStorageResource" /> in an executable-backed surrogate so that a locally
/// installed Azurite process can be started by DCP without requiring a container runtime.
/// </summary>
/// <remarks>
/// <para>
/// The surrogate shares the parent resource's <see cref="ResourceAnnotationCollection"/> so that all
/// endpoint, health-check, and lifecycle annotations added by
/// <see cref="AzureStorageExtensions.RunAsLocalEmulator"/> land on the same annotation bag that
/// connection-string and Azure Functions configuration code reads from.
/// </para>
/// <para>
/// The <see cref="ExecutableResource"/> base constructor requires a non-empty command placeholder;
/// the real command (path to the private Azurite install) is resolved and injected during the
/// <see cref="ApplicationModel.BeforeResourceStartedEvent"/> before DCP launches the process.
/// </para>
/// </remarks>
/// <param name="innerResource">The parent Azure Storage resource whose annotation bag is shared.</param>
public sealed class AzureStorageLocalEmulatorResource(AzureStorageResource innerResource)
    // Use a placeholder command; the actual path is resolved in the pre-start hook before DCP launches.
    // The DCP resource name includes the '-azurite' suffix so this resource can be registered
    // alongside the parent AzureStorageResource without triggering the duplicate-name guard.
    : ExecutableResource(innerResource.Name + AzureStorageLocalEmulatorResource.DcpNameSuffix,
                         AzureStorageLocalEmulatorResource.PlaceholderCommand,
                         string.Empty),
      IResourceWithParent<AzureStorageResource>
{
    internal const string PlaceholderCommand = "<azurite-not-yet-resolved>";

    /// <summary>
    /// Suffix appended to the parent resource name to form the DCP registration name for this surrogate.
    /// The suffix keeps the name unique in the model while still making it easy to identify which
    /// storage resource this process backs.
    /// </summary>
    internal const string DcpNameSuffix = "-azurite";

    private readonly AzureStorageResource _innerResource = innerResource ?? throw new ArgumentNullException(nameof(innerResource));

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to the parent resource so every annotation (endpoints, health checks, emulator marker,
    /// lifecycle hooks) written via either builder is visible in a single consistent place.
    /// </remarks>
    public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;

    /// <inheritdoc />
    public AzureStorageResource Parent => _innerResource;
}
