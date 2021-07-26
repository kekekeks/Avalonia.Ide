import * as assert from 'assert';

import { serialize } from '../../previewer/avaloniaRemote/bsonSerializer';
import { UpdateXamlMessage, UpdateXamlResultMessage } from '../../previewer/avaloniaRemote/messages/designMessages';

suite('Extension Test Suite 2', () => {
	test('Serialzier test 1', () => {
		console.log("Runnign test  oneoneone");
		const msg = new UpdateXamlMessage();
		msg.Xaml = "X";
		msg.AssemblyPath = "A";
		msg.XamlFileProjectPath = "F";

		const bytes = serialize(msg);
		const bytesArray = [...bytes.slice(0, 10)];

		let data = [64, 0, 0, 0, 2, 88, 97, 109, 108, 0, 2, 0, 0, 0, 88, 0, 2, 65, 115, 115, 101, 109, 98, 108, 121, 80, 97, 116, 104, 0, 2, 0, 0, 0, 65, 0, 2, 88, 97, 109, 108, 70, 105, 108, 101, 80, 114, 111, 106, 101, 99, 116, 80, 97, 116, 104, 0, 2, 0, 0, 0, 70, 0, 0];

		data = data.slice(0, 10);
		console.log("Actual");

		console.log(bytesArray);
		console.log("Expected");
		console.log(data);

		assert.strictEqual(bytesArray, data);

	});

	
	test('Serialzier test 2', () => {

		const msg = new UpdateXamlResultMessage();
		msg.Error = "E",
		msg.Handle = "H";

		const bytes = serialize(msg);
		const bytesArray = [...bytes];

		const data = [43, 0, 0, 0, 2, 69, 114, 114, 111, 114, 0, 2, 0, 0, 0, 69, 0, 2, 72, 97, 110, 100, 108, 101, 0, 2, 0, 0, 0, 72, 0, 10, 69, 120, 99, 101, 112, 116, 105, 111, 110, 0, 0];

		console.log("Actual");
		console.log(bytes);
		console.log("Expected");
		console.log(data);

		assert.strictEqual(bytesArray, data);

	});
});
