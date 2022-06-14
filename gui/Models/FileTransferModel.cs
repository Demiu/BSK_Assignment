using System;
using System.Threading.Tasks;
using ReactiveUI;

namespace Gui.Models;

public class FileTransferModel : ReactiveObject
{
    Lib.Fs.Transfer handle;
    double _value;

    public string Name => handle.FilePath;
    public double Value {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public FileTransferModel(Lib.Fs.Transfer handle) {
        this.handle = handle;
        Lib.Util.TaskRunSafe(async () => {
            while (handle.CurrentSize < handle.TotalSize) {
                await Task.Delay(500);
                var p = (double)handle.CurrentSize / handle.TotalSize * 100;
                await Console.Out.WriteLineAsync($"newprog {p}");
                Value = p;
            }
        });
    }
}
