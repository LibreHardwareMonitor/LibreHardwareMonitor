using System;
using System.Threading;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Mutexes class: Manages common mutexes
/// </summary>
internal static class Mutexes
{
    private static Mutex _ecMutex;
    private static Mutex _isaBusMutex;
    private static Mutex _pciBusMutex;
    private static Mutex _razerMutex;

    /// <summary>
    /// Opens the mutexes.
    /// </summary>
    public static void Open()
    {
        _isaBusMutex = CreateOrOpenExistingMutex("Global\\Access_ISABUS.HTP.Method");
        _pciBusMutex = CreateOrOpenExistingMutex("Global\\Access_PCI");
        _ecMutex = CreateOrOpenExistingMutex("Global\\Access_EC");
        _razerMutex = CreateOrOpenExistingMutex("Global\\RazerReadWriteGuardMutex");
    }

    /// <summary>
    /// Closes the mutexes.
    /// </summary>
    public static void Close()
    {
        _isaBusMutex?.Close();
        _pciBusMutex?.Close();
        _ecMutex?.Close();
        _razerMutex?.Close();
    }

    #region ISA Bus
    
    /// <summary>
    /// Wait: ISA bus
    /// </summary>
    /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
    /// <returns></returns>
    public static bool WaitIsaBus(int millisecondsTimeout) => WaitMutex(_isaBusMutex, millisecondsTimeout);

    /// <summary>
    /// Release: ISA bus
    /// </summary>
    public static void ReleaseIsaBus()
    {
        _isaBusMutex?.ReleaseMutex();
    }

    #endregion

    #region PCI Bus
    
    /// <summary>
    /// Wait: PCI bus
    /// </summary>
    /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
    /// <returns></returns>
    public static bool WaitPciBus(int millisecondsTimeout) => WaitMutex(_pciBusMutex, millisecondsTimeout);

    /// <summary>
    /// Release: PCI bus
    /// </summary>
    public static void ReleasePciBus()
    {
        _pciBusMutex?.ReleaseMutex();
    }

    #endregion

    #region EC
    
    /// <summary>
    /// Wait: EC
    /// </summary>
    /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
    /// <returns></returns>
    public static bool WaitEc(int millisecondsTimeout) => WaitMutex(_ecMutex, millisecondsTimeout);

    /// <summary>
    /// Release: EC
    /// </summary>
    public static void ReleaseEc()
    {
        _ecMutex?.ReleaseMutex();
    }

    #endregion

    #region Razer
    
    /// <summary>
    /// Wait: Razer
    /// </summary>
    /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
    /// <returns></returns>
    public static bool WaitRazer(int millisecondsTimeout) => WaitMutex(_razerMutex, millisecondsTimeout);

    /// <summary>
    /// Release: Razer
    /// </summary>
    public static void ReleaseRazer()
    {
        _razerMutex?.ReleaseMutex();
    }

    #endregion

    /// <summary>
    /// Creates or opens a Mutex.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    private static Mutex CreateOrOpenExistingMutex(string name)
    {
        try
        {
            return new Mutex(false, name);
        }
        catch (UnauthorizedAccessException)
        {
            try
            {
                return Mutex.OpenExisting(name);
            }
            catch
            {
                // Ignored.
            }
        }

        return null;
    }

    /// <summary>
    /// Wait: Mutex
    /// </summary>
    /// <param name="mutex">The mutex.</param>
    /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
    /// <returns></returns>
    private static bool WaitMutex(Mutex mutex, int millisecondsTimeout)
    {
        if (mutex == null) return true;

        try
        {
            return mutex.WaitOne(millisecondsTimeout, false);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
