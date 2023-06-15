using System;
using System.Threading;

namespace LibreHardwareMonitor.Hardware;

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

        static Mutex CreateOrOpenExistingMutex(string name)
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

    public static bool WaitIsaBus(int millisecondsTimeout)
    {
        return WaitMutex(_isaBusMutex, millisecondsTimeout);
    }

    public static void ReleaseIsaBus()
    {
        _isaBusMutex?.ReleaseMutex();
    }

    public static bool WaitPciBus(int millisecondsTimeout)
    {
        return WaitMutex(_pciBusMutex, millisecondsTimeout);
    }

    public static void ReleasePciBus()
    {
        _pciBusMutex?.ReleaseMutex();
    }

    public static bool WaitEc(int millisecondsTimeout)
    {
        return WaitMutex(_ecMutex, millisecondsTimeout);
    }

    public static void ReleaseEc()
    {
        _ecMutex?.ReleaseMutex();
    }

    public static bool WaitRazer(int millisecondsTimeout)
    {
        return WaitMutex(_razerMutex, millisecondsTimeout);
    }

    public static void ReleaseRazer()
    {
        _razerMutex?.ReleaseMutex();
    }

    private static bool WaitMutex(Mutex mutex, int millisecondsTimeout)
    {
        if (mutex == null)
            return true;

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
