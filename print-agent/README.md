# Maker Print Agent (Windows)

Agente local para integração com o Maker via HTTP, sem abrir páginas para imprimir.

## Recursos
- Listar impressoras instaladas.
- Testar conexão/saúde do agente.
- Impressão direta de texto.
- Impressão direta de arquivos (TXT, PDF e demais tipos via shell do Windows).
- Opção para iniciar automaticamente com o Windows (pergunta no início do executável).

## Endpoints
Base URL padrão: `http://127.0.0.1:17777`

### `GET /health`
Retorna status do agente.

### `GET /printers`
Lista impressoras e impressora padrão.

### `POST /print/text`
```json
{
  "text": "Texto para impressão",
  "printerName": "Nome da impressora (opcional)",
  "documentName": "Pedido #123 (opcional)",
  "copies": 1
}
```

### `POST /print/file`
```json
{
  "filePath": "C:\\temp\\arquivo.pdf",
  "printerName": "Nome da impressora (opcional)",
  "copies": 1,
  "deleteAfterPrint": false
}
```

### `POST /print/pdf`
```json
{
  "filePath": "C:\\temp\\etiqueta.pdf",
  "printerName": "Nome da impressora (opcional)",
  "sumatraPdfPath": "C:\\Apps\\SumatraPDF\\SumatraPDF.exe",
  "deleteAfterPrint": false
}
```

> Para PDF em impressoras térmicas, o mais confiável é ter o **SumatraPDF** instalado e acessível no `PATH` (ou enviar o caminho no payload).

## Publicar executável
No Windows com .NET SDK 8 instalado:

```bash
dotnet publish .\print-agent\PrintAgent.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Saída esperada:
`print-agent\bin\Release\net8.0-windows\win-x64\publish\PrintAgent.exe`


### Publicação com .bat (recomendado)
Dentro da pasta `print-agent`, execute:

```bat
publish-agent.bat
```

O script valida o `dotnet`, publica o executável e abre a pasta final automaticamente.

## Exemplo de integração Maker
1. Chamar `GET /health` para validar conexão.
2. Chamar `GET /printers` para exibir impressoras disponíveis.
3. Chamar `POST /print/text` ou `POST /print/pdf` conforme o caso.
