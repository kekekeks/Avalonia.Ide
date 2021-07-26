/* eslint-disable @typescript-eslint/naming-convention */
import { avaloniaRemoteMessageGuid } from "../avaloniaRemoteTypeRegistry";
import { Key } from "./key";

/* Keep this in sync with InputModifiers in the main library */
enum InputModifiers {
    Alt = 0,
    Control,
    Shift,
    Windows,
    LeftMouseButton,
    RightMouseButton,
    MiddleMouseButton
}

/* Keep this in sync with InputModifiers in the main library */
enum MouseButton {
    None = 0,
    Left,
    Right,
    Middle
}

abstract class InputEventMessageBase {
    public Modifiers: InputModifiers[] = [];
}

abstract class PointerEventMessageBase extends InputEventMessageBase {
    public X: number = 0;
    public Y: number = 0;
}

@avaloniaRemoteMessageGuid("6228F0B9-99F2-4F62-A621-414DA2881648")
export class PointerMovedEventMessage extends PointerEventMessageBase {

}

@avaloniaRemoteMessageGuid("7E9E2818-F93F-411A-800E-6B1AEB11DA46")
export class PointerPressedEventMessage extends PointerEventMessageBase {
    public Button: MouseButton | undefined;
}

@avaloniaRemoteMessageGuid("4ADC84EE-E7C8-4BCF-986C-DE3A2F78EDE4")
export class PointerReleasedEventMessage extends PointerEventMessageBase {
    public Button: MouseButton | undefined;
}

@avaloniaRemoteMessageGuid("79301A05-F02D-4B90-BB39-472563B504AE")
export class ScrollEventMessage extends PointerEventMessageBase {
    public DeltaX: number = 0;
    public DeltaY: number = 0;
}

@avaloniaRemoteMessageGuid("1C3B691E-3D54-4237-BFB0-9FEA83BC1DB8")
export class KeyEventMessage extends InputEventMessageBase {
    public IsDown: boolean = false;
    public Key: Key = Key.None;
}

@avaloniaRemoteMessageGuid("C174102E-7405-4594-916F-B10B8248A17D")
export class TextInputEventMessage extends InputEventMessageBase {
    public Text: string = "";
}