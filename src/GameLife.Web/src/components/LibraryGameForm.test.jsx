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
      { plataforma: 'PC', midiaFisica: false },
      { plataforma: 'STEAM', midiaFisica: false }
    ]
  });
});

test('permite mídia física diferente em cada plataforma', async () => {
  const onSubmit = renderForm();
  preencherTitulo();
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Selecionar Xbox Series X|S'));
  fireEvent.click(screen.getByLabelText('Mídia física em Xbox Series X|S'));

  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Graveyard Keeper',
    plataformas: [
      { plataforma: 'PC', midiaFisica: false },
      { plataforma: 'Xbox Series X|S', midiaFisica: true }
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

test('envia status horas e conquistas somente na plataforma acompanhada', async () => {
  const onSubmit = renderForm();
  preencherTitulo('Hades');
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Selecionar STEAM'));
  fireEvent.click(screen.getByLabelText('Acompanhar progresso em STEAM'));
  fireEvent.change(screen.getByLabelText('Status em STEAM'), { target: { value: 'Zerado' } });
  fireEvent.change(screen.getByLabelText('Horas jogadas em STEAM'), { target: { value: '24.5' } });
  fireEvent.change(screen.getByLabelText('Conquistas possíveis em STEAM'), { target: { value: '50' } });
  fireEvent.change(screen.getByLabelText('Conquistas obtidas em STEAM'), { target: { value: '42' } });

  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
  expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Hades',
    plataformas: [
      { plataforma: 'PC', midiaFisica: false },
      {
        plataforma: 'STEAM',
        midiaFisica: false,
        acompanhamento: {
          status: 'Zerado',
          tempoEmHoras: 24.5,
          conquistasPossiveis: 50,
          conquistasObtidas: 42
        }
      }
    ]
  });
});

test('bloqueia envio sem plataforma e conquistas inconsistentes', async () => {
  const onSubmit = renderForm();
  preencherTitulo();
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Selecione pelo menos uma plataforma.');
  expect(onSubmit).not.toHaveBeenCalled();

  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Acompanhar progresso em PC'));
  fireEvent.change(screen.getByLabelText('Conquistas possíveis em PC'), { target: { value: '5' } });
  fireEvent.change(screen.getByLabelText('Conquistas obtidas em PC'), { target: { value: '6' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar à biblioteca' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Conquistas obtidas não podem superar as possíveis.');
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
        { posseId: 1, nome: 'PC', midiaFisica: true, acompanhamento: null },
        {
          posseId: 2,
          nome: 'STEAM',
          midiaFisica: false,
          acompanhamento: {
            status: 'NaoTerminado',
            dataStatus: '2026-11-10T00:00:00',
            tempoEmHoras: 12,
            conquistasPossiveis: 20,
            conquistasObtidas: 5
          }
        }
      ]
    }}
  />);

  expect(screen.getByLabelText('Nome do jogo')).toHaveValue('Graveyard Keeper');
  expect(screen.getByLabelText('Ano de lançamento (opcional)')).toHaveValue(2018);
  expect(screen.getByLabelText('Selecionar PC')).toBeChecked();
  expect(screen.getByLabelText('Mídia física em PC')).toBeChecked();
  expect(screen.getByLabelText('Selecionar STEAM')).toBeChecked();
  expect(screen.getByLabelText('Acompanhar progresso em STEAM')).toBeChecked();
  expect(screen.getByLabelText('Horas jogadas em STEAM')).toHaveValue(12);

  fireEvent.click(screen.getByLabelText('Selecionar Xbox Series X|S'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [
      { plataforma: 'PC', midiaFisica: true },
      {
        plataforma: 'STEAM',
        midiaFisica: false,
        acompanhamento: {
          status: 'NaoTerminado',
          dataStatus: '2026-11-10',
          tempoEmHoras: 12,
          conquistasPossiveis: 20,
          conquistasObtidas: 5
        }
      },
      { plataforma: 'Xbox Series X|S', midiaFisica: false }
    ]
  }));
});
