﻿using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using SKExp.ConsoleApp8.Utils;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace SKExp.ConsoleApp8.Exp
{
	internal class Exp4_Templatizing_Prompts
	{
		new public static async Task RunAsync()
		{
			IKernelBuilder builder = SKHelper.GetBuilder();

			Kernel kernel = builder.Build();


			// Create chat history
			ChatHistory history = [];

			// Create choices
			List<string> choices = ["ContinueConversation", "EndConversation"];

			// Create few-shot examples
			List<ChatHistory> fewShotExamples = [
				[
					new ChatMessageContent(AuthorRole.User, "Can you send a very quick approval to the marketing team?"),
					new ChatMessageContent(AuthorRole.System, "Intent:"),
					new ChatMessageContent(AuthorRole.Assistant, "ContinueConversation")
				],
				[
					new ChatMessageContent(AuthorRole.User, "Thanks, I'm done for now"),
					new ChatMessageContent(AuthorRole.System, "Intent:"),
					new ChatMessageContent(AuthorRole.Assistant, "EndConversation")
				]
			];

			// Create handlebars template for intent
			var getIntent = kernel.CreateFunctionFromPrompt(
				new ()
				{
					Template = @"
        <message role=""system"">Instructions: What is the intent of this request?
        Do not explain the reasoning, just reply back with the intent. If you are unsure, reply with ContinueConversation.
        Choices: {{choices}}.</message>

        {{#each fewShotExamples}}
            {{#each this}}
                <message role=""{{role}}"">{{content}}</message>
            {{/each}}
        {{/each}}

        {{#each chatHistory}}
            <message role=""{{role}}"">{{content}}</message>
        {{/each}}

        <message role=""user"">{{request}}</message>
        <message role=""system"">Intent:</message>",
					TemplateFormat = "handlebars"
				},
				new HandlebarsPromptTemplateFactory()
			);

			// Create a Semantic Kernel template for chat
			var chat = kernel.CreateFunctionFromPrompt(
				@"{{$history}}
    User: {{$request}}
    Assistant: "
			);

			// Start the chat loop
			while (true)
			{
				Console.WriteLine("---------------------------------------------");
				// Get user input
				Console.Write("User > ");
				var request = Console.ReadLine();

				// Invoke prompt
				var intent = await kernel.InvokeAsync(
					getIntent,
					new() {
			{ "request", request },
			{ "choices", choices },
			{ "history", history },
			{ "fewShotExamples", fewShotExamples }
					}
				);

				// End the chat if the intent is "Stop"
				if (intent.ToString() == "EndConversation")
				{
					break;
				}

				// Get chat response
				var chatResult = kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
					chat,
					new() {
			{ "request", request },
			{ "history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) }
					}
				);

				// Stream the response
				string message = "";
				await foreach (var chunk in chatResult)
				{
					if (chunk.Role.HasValue) Console.Write(chunk.Role + " > ");
					message += chunk;
					Console.Write(chunk);
				}
				Console.WriteLine();

				// Append to history
				history.AddUserMessage(request!);
				history.AddAssistantMessage(message);
				Console.WriteLine();
			}



		}	
	}
}
