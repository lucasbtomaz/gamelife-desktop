import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import LibraryGameScreen from './LibraryGameScreen.jsx';

const platforms = ['PC', 'STEAM', 'XBOX'];

test('mostra cadastro em tela própria e permite voltar', () => {
  const onBack = vi.fn();
  render(<LibraryGameScreen
    game={null}
    platforms={platforms}
    saving={false}
    onSubmit={vi.fn()}
    onBack={onBack}
  />);

  expect(screen.getByRole('heading', { name: 'Adicionar jogo' })).toBeInTheDocument();
  expect(screen.getByLabelText('Nome do jogo')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Voltar para a biblioteca' }));
  expect(onBack).toHaveBeenCalledTimes(1);
});

test('mostra somente a ficha selecionada na edição', () => {
  render(<LibraryGameScreen
    game={{
      jogoId: 34,
      titulo: 'Graveyard Keeper',
      anoLancamento: null,
      plataformas: [{ posseId: 30, nome: 'Steam', midiaFisica: false, acompanhamento: null }]
    }}
    platforms={platforms}
    saving={false}
    onSubmit={vi.fn()}
    onBack={vi.fn()}
  />);

  expect(screen.getByRole('heading', { name: 'Editar Graveyard Keeper' })).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar STEAM')).toBeChecked();
  expect(screen.getByRole('button', { name: 'Salvar alterações' })).toBeInTheDocument();
});
