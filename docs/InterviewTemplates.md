# Writing Interview Templates

The bot can automatically interview users when they open a ticket. These interviews are defined in interview templates. 
The templates each apply to a single ticket category so you can have different interviews depending on the type of ticket.

## Interview Template Commands

Whether you already have a template or not the first step is always to use the `/interviewtemplate get <category>` command on the 
ticket category you want to edit. If you haven't yet, use the `/addcategory` command on the category to register it with the bot.

The bot will reply with a JSON template file which you can edit and upload using the `/interviewtemplate set` command.

You can also delete an interview template using the `/interviewtemplate delete` command.

## Writing Your First Interview Template

Use the get command to get a default template for the ticket category you want to edit:
```
/interviewtemplate get <category>
```

The bot will reply with a JSON template file for you to edit using your preferred text editor.

**Note:** It is highly recommended to use integrated template validation in your text editor, see below.

When you are done editing it you can upload it to the bot using the set command:
```
/interviewtemplate set <category>
```

The bot will check that your template is correctly formatted and provide feedback if it is not.

## Automatic Template Validation and Suggestions in Text Editors

It is highly recommended to use the interview template JSON schema to get live validation of your template while you write it:

### Guides for Different Editors:

<details>

<summary>VS Code</summary>

1. Go to `File->Preferences->Settings`.
2. Search for `json schema`.
3. Click `Edit in settings.json` on the schema setting.
4. Set the `json.schemas` property to the following to automatically validate template files:
```json
{
    "json.schemas":
    [
        {
            "fileMatch":
            [
                "interview-template*.json"
            ],
            "url": "https://raw.githubusercontent.com/KarlOfDuty/SupportBoi/refs/heads/main/Interviews/interview_template.schema.json"
        }
    ]
}
```
5. Open an interview template, you should now get suggestions for things like message types and color names, and error highlighting for any invalid sections.

</details>

<details>

<summary>Jetbrains Editors</summary>

1. Go to `File->Settings->Languages & Frameworks->Schemas->JSON Schema Mapping`.
2. Add a new schema with the following URL: `https://raw.githubusercontent.com/KarlOfDuty/SupportBoi/refs/heads/main/Interviews/interview_template.schema.json`.
   ![Schema settings](./img/riderJSONSchema.png)
3. Restart your editor and all interview templates should now automatically be set to the correct schema in the bottom right of the window.

</details>

### Example Usage:

![Auto Completion Example](./img/autoCompletionExample.png) ![Validation Example](./img/validationExample.png)

## Interview Template Format

This section lists all the properties that can be used in an interview template.
If you have set up your editor as suggested above it will handle a lot of this for you automatically.

```json
{
  "category-id": "1006863882301755503",
  "interview":
  {
    "message": "What is your favourite colour?",
    "message-type": "BUTTONS",
    "color": "BLUE",
    "summary-field": "Favourite colour",
    "paths":
    {
      "PRIMARY":
      {
        "message": "Summary",
        "message-type": "END_WITH_SUMMARY",
        "color": "BLUE",
        "button-style": "PRIMARY",
        "paths": {}
      },
      "SECONDARY":
      {
        "message": "Summary",
        "message-type": "END_WITH_SUMMARY",
        "color": "GRAY",
        "button-style": "SECONDARY",
        "paths": {}
      },
      "SUCCESS":
      {
        "message": "Summary",
        "message-type": "END_WITH_SUMMARY",
        "color": "GREEN",
        "button-style": "SUCCESS",
        "paths": {}
      },
      "DANGER":
      {
        "message": "Summary",
        "message-type": "END_WITH_SUMMARY",
        "color": "RED",
        "button-style": "DANGER",
        "paths": {}
      }
    }
  }
}
```

### Template Root

| Property&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Required | Description                                                                                                                          |
|----------------------------------------------------------------------------|----------|--------------------------------------------------------------------------------------------------------------------------------------|
| `category-id`                                                              | Yes      | The id of the category this template applies to. You can change this and re-upload the template to apply it to a different category. |
| `interview`                                                                | Yes      | Contains the interview conversation tree, starting with one path which branches into many.                                           |

### Interview Paths

<!-- This is an HTML table to allow for markdown formatting inside -->
<table>
  <tbody>
    <tr>
      <th>Property&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</th>
      <th>Required</th>
      <th>Type</th>
      <th>Description</th>
    </tr>
    <tr>
      <td>
<!-- For whatever reason there must be an empty row here -->

`message`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
The text in the embed message that will be sent to the user when they reach this step.
      </td>
    </tr>
    <tr>
      <td>
`message-type`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
The type of message, decides what the bot will do when the user gets to this step.

<!-- For whatever reason this tag cannot be indented -->
</td>
    </tr>
    <tr>
      <td>
<!-- For whatever reason there must be an empty row here -->

`color`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>

Colour of the message embed. You can either enter a colour name or a hexadecimal RGB value.
      </td>
    </tr>
    <tr>
      <td>
`paths`
      </td>
      <td>Yes</td>
      <td>Steps</td>
      <td>
One or more interview steps. The name of the step is used as a regex match against the user's answer,
except for selection boxes and buttons where each step becomes a button or selection option.
      </td>
    </tr>
    <tr>
      <td>
`heading`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
The title of the embed message.
      </td>
    </tr>
    <tr>
      <td>
`summary-field`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
When an interview ends all previous answers with this property will be put in a summary.
If this property is not specified the answer will not be shown in the summary.
The value of this property is the name which will be displayed next to the answer in the summary.
      </td>
    </tr>
    <tr>
      <td>
`button-style`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
The style of this path's button. Requires that the parent path is a `BUTTONS` path.
Must be one of the following:
- `PRIMARY`
- `SECONDARY`
- `SUCCESS`
- `DANGER`

Default style is `SECONDARY`.

![Button Example](./img/buttonExample.png)
      </td>
    </tr>
    <tr>
      <td>
`selector-description`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
Description for this option in the parent path's selection box. Requires that the parent path is a `TEXT_SELECTOR`.
      </td>
    </tr>
    <tr>
      <td>
`selector-placeholder`
      </td>
      <td>Yes</td>
      <td>String</td>
      <td>
The placeholder text shown before a value is selected in the selection box. Requires that this path is a `TEXT_SELECTOR`.
      </td>
    </tr>
    <tr>
      <td>
`max-length`
      </td>
      <td>Yes</td>
      <td>Number</td>
      <td>
The maximum length of the user's response message. Requires that this path is a `TEXT_INPUT`.
      </td>
    </tr>
    <tr>
      <td>
`min-length`
      </td>
      <td>Yes</td>
      <td>Number</td>
      <td>
The minimum length of the user's response message. Requires that this path is a `TEXT_INPUT`.
      </td>
    </tr>
  </tbody>
</table>

### Message Types

| Message Type           | Description                                                                                                                              |
|------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| `ERROR`                | Sends an error message but does not stop the interview. The interview remains on the same step as before allowing the user to try again. |
| `END_WITH_SUMMARY`     | End the interview and create a summary of the answers.                                                                                   |
| `END_WITHOUT_SUMMARY`  | End the interview with a simple message without a summary.                                                                               |
| `BUTTONS`              | Creates a message with one button per child path where the button text is the name of the child path.                                    |
| `TEXT_SELECTOR`        | Creates a selection box with one option per child path where the option text is the name of the child path.                              |
| `USER_SELECTOR`        | Creates a selection box where the user can select a user from the Discord server. The value used for the summary is the user's mention.  |
| `ROLE_SELECTOR`        | Same as above but for a role.                                                                                                            |
| `MENTIONABLE_SELECTOR` | Same as above but works for both roles and users.                                                                                        |
| `CHANNEL_SELECTOR`     | Same as above but for channels and categories.                                                                                           |
| `TEXT_INPUT`           | Lets the user reply to the bot message with their own text.                                                                              |
