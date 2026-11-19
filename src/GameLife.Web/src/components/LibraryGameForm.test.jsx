import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import LibraryGameForm from './LibraryGameForm.jsx';

const plataformas = ['PC', 'STEAM', 'Xbox Series X|S'];

function renderForm(onSubmit = vi.fn().mockResolvedValue(undefined)) {
  render(<LibraryGameForm platforms={plataformas} saving={false} onSubmit={onSubmit} />);
  return onSubmit;
}

function preencherTitulo(titulo = 'Graveyard Keeper') {
  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: titulo } });
}

afterEach(() => vi.restoreAllMocks());

test('seleciona várias plataformas e envia uma única ficha', async () => {
  const onSubmit = renderForm();
  preencherTitulo();
  fireEvent.change(screen.getByLabelText('Ano de lançamento (opcional)'), { target: { value: '2018' } });
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Selecionar STEAM'));
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
  expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [
      { plataforma: 'PC', compraDigital: true, midiaFisica: false, porAssinatura: false },
      { plataforma: 'STEAM', compraDigital: true, midiaFisica: false, porAssinatura: false }
    ]
  });
});

test('permite mídia física e compra digital diferentes em cada plataforma', async () => {
  const onSubmit = renderForm();
  preencherTitulo();
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Selecionar Xbox Series X|S'));
  fireEvent.click(screen.getByLabelText('Compra digital em Xbox Series X|S'));
  fireEvent.click(screen.getByLabelText('Mídia física em Xbox Series X|S'));
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Graveyard Keeper',
    plataformas: [
      { plataforma: 'PC', compraDigital: true, midiaFisica: false, porAssinatura: false },
      { plataforma: 'Xbox Series X|S', compraDigital: false, midiaFisica: true, porAssinatura: false }
    ]
  }));
});

test('mantém acompanhamento completamente opcional', async () => {
  const onSubmit = renderForm();
  preencherTitulo('Celeste');
  fireEvent.click(screen.getByLabelText('Selecionar STEAM'));
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
  expect(onSubmit.mock.calls[0][0].plataformas[0]).not.toHaveProperty('acompanhamento');
});

test('bloqueia envio sem plataforma, e sem forma de acesso', async () => {
  const onSubmit = renderForm();
  preencherTitulo();
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Selecione pelo menos uma plataforma.');
  expect(onSubmit).not.toHaveBeenCalled();

  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Compra digital em PC'));
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Selecione pelo menos uma forma de acesso para PC.');
  expect(onSubmit).not.toHaveBeenCalled();
});

test('preenche uma ficha existente e permite marcar outra plataforma', async () => {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(<LibraryGameForm
    platforms={plataformas}
    saving={false}
    onSubmit={onSubmit}
    initialGame={{
      jogoId: 34,
      titulo: 'Graveyard Keeper',
      anoLancamento: 2018,
      plataformas: [
        { posseId: 1, nome: 'PC', compraDigital: false, midiaFisica: true, porAssinatura: false, acompanhamento: null },
        {
          posseId: 2,
          nome: 'Steam',
          compraDigital: true,
          midiaFisica: false,
          porAssinatura: false,
          acompanhamento: {
            status: 'NaoTerminado',
            dataStatus: '2026-11-10T00:00:00',
            tempoEmHoras: 12,
            conquistasPossiveis: 20,
            conquistasObtidas: 5
          }
        }
      ],
      assinaturas: []
    }}
  />);

  expect(screen.getByLabelText('Nome do jogo')).toHaveValue('Graveyard Keeper');
  expect(screen.getByLabelText('Ano de lançamento (opcional)')).toHaveValue(2018);
  expect(screen.getByLabelText('Selecionar PC')).toBeChecked();
  expect(screen.getByLabelText('Mídia física em PC')).toBeChecked();
  expect(screen.getByLabelText('Selecionar STEAM')).toBeChecked();
  expect(screen.queryByLabelText('Acompanhar progresso em STEAM')).not.toBeInTheDocument();
  expect(screen.queryByLabelText('Horas jogadas em STEAM')).not.toBeInTheDocument();

  fireEvent.click(screen.getByLabelText('Selecionar Xbox Series X|S'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [
      { plataforma: 'PC', compraDigital: false, midiaFisica: true, porAssinatura: false },
      {
        plataforma: 'STEAM',
        compraDigital: true,
        midiaFisica: false,
        porAssinatura: false
      },
      { plataforma: 'Xbox Series X|S', compraDigital: true, midiaFisica: false, porAssinatura: false }
    ]
  }));
});

test('permite cadastrar jogo exclusivo por assinatura e também compra simultânea', async () => {
  const onSubmit = renderForm();
  preencherTitulo('Hi-Fi Rush');
  fireEvent.click(screen.getByLabelText('Selecionar Xbox Series X|S'));
  fireEvent.click(screen.getByLabelText('Compra digital em Xbox Series X|S'));
  fireEvent.click(screen.getByLabelText('Assinatura em Xbox Series X|S'));

  fireEvent.change(screen.getByLabelText('Serviço de assinatura'), { target: { value: 'Game Pass' } });
  fireEvent.change(screen.getByLabelText('Situação da disponibilidade'), { target: { value: 'Disponível' } });
  fireEvent.change(screen.getByLabelText('Data de entrada'), { target: { value: '2026-01-15' } });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar assinatura' }));

  // Também marca compra digital simultaneamente
  fireEvent.click(screen.getByLabelText('Compra digital em Xbox Series X|S'));
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Hi-Fi Rush',
    plataformas: [
      { plataforma: 'Xbox Series X|S', compraDigital: true, midiaFisica: false, porAssinatura: true }
    ],
    assinaturas: [
      {
        servico: 'Game Pass',
        situacao: 'Disponível',
        dataDisponibilidade: '2026-01-15',
        dataEncerramento: null
      }
    ]
  }));
});

test('carrega assinatura compartilhada da Lista de Desejos ao informar o nome do jogo', async () => {
  global.fetch = vi.fn().mockResolvedValueOnce({
    ok: true,
    json: async () => [{
      id: 7,
      jogoId: 90,
      servico: 'Game Pass',
      situacao: 'Disponível',
      dataDisponibilidade: '2026-10-01T00:00:00',
      dataEncerramento: null
    }]
  });

  renderForm();
  const input = screen.getByLabelText('Nome do jogo');
  fireEvent.change(input, { target: { value: 'Persona 3 Reload' } });
  fireEvent.blur(input);

  expect(await screen.findByText('Game Pass · Disponível')).toBeInTheDocument();
  expect(fetch).toHaveBeenCalledWith('/api/assinaturas?titulo=Persona%203%20Reload');
});

test('envia resgate sem vínculo com o catálogo', async () => {
  const onSubmit = renderForm();
  preencherTitulo('Jogo mensal');
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Compra digital em PC'));
  fireEvent.click(screen.getByLabelText('Resgatado da assinatura em PC'));
  fireEvent.change(screen.getByLabelText('Serviço exigido em PC'), { target: { value: 'PlayStation Plus' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Jogo mensal',
    plataformas: [{ plataforma: 'PC', compraDigital: false, midiaFisica: false,
      porAssinatura: true, resgatadoDeAssinatura: true, servicoExigido: 'PlayStation Plus' }]
  }));
});

test('carrega e permite desmarcar o resgate existente', async () => {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(<LibraryGameForm platforms={plataformas} saving={false} onSubmit={onSubmit}
    initialGame={{ titulo: 'Jogo mensal', plataformas: [{ nome: 'PC', compraDigital: true,
      porAssinatura: true, resgatadoDeAssinatura: true, servicoExigido: 'PlayStation Plus' }] }} />);
  expect(screen.getByLabelText('Resgatado da assinatura em PC')).toBeChecked();
  expect(screen.getByLabelText('Serviço exigido em PC')).toHaveValue('PlayStation Plus');
  fireEvent.click(screen.getByLabelText('Resgatado da assinatura em PC'));
  fireEvent.click(screen.getByRole('button', { name: /salvar/i }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Jogo mensal',
    plataformas: [{ plataforma: 'PC', compraDigital: true, midiaFisica: false,
      porAssinatura: false, resgatadoDeAssinatura: false }]
  }));
});
